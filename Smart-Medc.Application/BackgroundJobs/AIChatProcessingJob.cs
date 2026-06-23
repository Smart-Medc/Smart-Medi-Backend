using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Smart_Medc.API.Hubs;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.Interfaces;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Smart_Medc.Application.BackgroundJobs
{
    public class AIChatProcessingJob
    {
        private readonly IAIChatService _aiChatService;
        private readonly IFileStorageService _fileStorage;
        private readonly IHubContext<AIChatHub> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AIChatProcessingJob> _logger;
        private readonly string _cloudRunUrl;

        public AIChatProcessingJob(
            IAIChatService aiChatService,
            IFileStorageService fileStorage,
            IHubContext<AIChatHub> hubContext,
            IHttpClientFactory httpClientFactory,
            ILogger<AIChatProcessingJob> logger,
            IConfiguration configuration)
        {
            _aiChatService = aiChatService;
            _fileStorage = fileStorage;
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cloudRunUrl = configuration["CloudRunSettings:BaseUrl"]
                ?? "https://medgemma-ai-xxxxx.a.run.app";
        }

        public async Task ProcessChatMessageAsync(
            Guid patientId,
            Guid sessionId,
            Guid aiMessageId,
            string userMessage,
            bool useMedicalRecords,
            List<string> attachmentStoragePaths,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation(
                    $"Processing AI chat: Patient={patientId}, Session={sessionId}, Message={aiMessageId}");

                var medicalContext = "";
                if (useMedicalRecords)
                {
                    // Build medical context from patient records
                    medicalContext = await BuildMedicalContextAsync(patientId, ct);
                }

                // Generate presigned URLs for attachments
                var presignedUrls = new List<string>();
                foreach (var path in attachmentStoragePaths)
                {
                    var url = await _fileStorage.GeneratePresignedUrlAsync(
                        "ai-chat-attachments", path, TimeSpan.FromHours(1), ct);
                    presignedUrls.Add(url);
                }

                // Call Cloud Run endpoint
                var httpClient = _httpClientFactory.CreateClient();
                var requestBody = new
                {
                    message = userMessage,
                    medical_records_context = medicalContext,
                    file_urls = presignedUrls
                };

                var response = await httpClient.PostAsJsonAsync(
                    $"{_cloudRunUrl}/api/chat/stream",
                    requestBody,
                    cancellationToken: ct);

                response.EnsureSuccessStatusCode();

                // Stream response
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);

                var fullResponse = "";
                string? line;

                while ((line = await reader.ReadLineAsync(ct)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var json = JsonDocument.Parse(line);
                        if (json.RootElement.TryGetProperty("chunk", out var chunkElement))
                        {
                            var chunk = chunkElement.GetString() ?? "";
                            fullResponse += chunk;

                            // Send to SignalR clients
                            await _hubContext.Clients
                                .Group($"ai-response-{aiMessageId}")
                                .SendAsync("aiResponseChunk", new
                                {
                                    messageId = aiMessageId,
                                    chunk = chunk,
                                    isComplete = false
                                }, cancellationToken: ct);

                            await Task.Delay(50, ct); // Small delay for better streaming effect
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning($"Could not parse Cloud Run response: {line}");
                    }
                }

                // Save AI response
                await _aiChatService.SaveAIResponseAsync(
                    patientId, sessionId, aiMessageId, fullResponse, ct);

                // Send completion
                await _hubContext.Clients
                    .Group($"ai-response-{aiMessageId}")
                    .SendAsync("aiResponseChunk", new
                    {
                        messageId = aiMessageId,
                        chunk = "",
                        isComplete = true,
                        tokensUsed = EstimateTokens(fullResponse)
                    }, cancellationToken: ct);

                _logger.LogInformation($"AI processing completed: {aiMessageId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing AI chat: {aiMessageId}");

                await _hubContext.Clients
                    .Group($"ai-response-{aiMessageId}")
                    .SendAsync("aiResponseError", new
                    {
                        messageId = aiMessageId,
                        error = "Failed to generate response. Please try again."
                    });
            }
        }

        private async Task<string> BuildMedicalContextAsync(Guid patientId, CancellationToken ct)
        {
            var context = new StringBuilder();
            context.AppendLine("PATIENT MEDICAL SUMMARY:");
            context.AppendLine("=".PadRight(50, '='));

            // TODO: Fetch from repositories
            // - Medications (active)
            // - Recent medical records
            // - Chronic conditions
            // - Allergies
            // - Lab results

            return context.ToString();
        }

        private int EstimateTokens(string text) => (text.Length / 4) + 1;
    }
}