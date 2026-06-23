// Smart_Medc.Application/Services/AI/AIChatService.cs

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.DTOs.MedicalRecord;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace Smart_Medc.Application.Services.AI
{
    public class AIChatService : IAIChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<AIChatService> _logger;
        private readonly HttpClient _aiClient;
        private readonly IMedicalRecordService _medicalRecordService;
        private readonly IChatHubDispatcher _hubDispatcher;

        private const string AiChatContainer = "ai-chat-attachments";
        private const int MaxContextMessages = 10;  // previous messages to include in prompt

        public AIChatService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            ILogger<AIChatService> logger,
            HttpClient aiClient,
            IMedicalRecordService medicalRecordService,
            IChatHubDispatcher hubDispatcher)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _logger = logger;
            _aiClient = aiClient;
            _medicalRecordService = medicalRecordService;
            _hubDispatcher = hubDispatcher;
        }

        public async Task<ServiceResult<AIChatSessionDto>> CreateSessionAsync(
            Guid patientId, CreateSessionRequestDto dto, CancellationToken ct)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient == null)
                return ServiceResult<AIChatSessionDto>.NotFound("Patient not found");

            var session = new AIChatSession
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Title = dto.Title,
                UseMedicalRecordsContext = dto.UseMedicalRecordsContext,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AIChatSessions.AddAsync(session, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<AIChatSessionDto>.Success(MapToSessionDto(session));
        }

        public async Task<ServiceResult<AIChatMessageDto>> SendMessageAsync(
            Guid sessionId, string content, List<IFormFile>? files,
            string? connectionId, CancellationToken ct)
        {
            // 1. Validate session
            var session = await _unitOfWork.AIChatSessions
                .GetByIdWithMessagesAsync(sessionId, ct);
            if (session == null || session.IsDeleted)
                return ServiceResult<AIChatMessageDto>.NotFound("Session not found");

            // 2. Store user message
            var userMessage = new AIChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.User,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.AIChatMessages.AddAsync(userMessage, ct);

            // 3. Upload attachments (files) and create DB records
            var attachmentEntities = new List<AIChatMessageAttachment>();
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    using var stream = file.OpenReadStream();
                    var uploadResult = await _fileStorage.UploadFileAsync(
                        stream, file.FileName, file.ContentType, AiChatContainer, ct);

                    var attachment = new AIChatMessageAttachment
                    {
                        Id = Guid.NewGuid(),
                        MessageId = userMessage.Id,
                        FileName = file.FileName,
                        StoragePath = uploadResult.StoragePath,
                        ContentType = file.ContentType,
                        FileSizeBytes = uploadResult.FileSizeBytes,
                        UploadedAt = DateTime.UtcNow
                    };
                    attachmentEntities.Add(attachment);
                }
                if (attachmentEntities.Any())
                {
                    await _unitOfWork.AIChatMessageAttachments.AddRangeAsync(attachmentEntities, ct);
                    // Link attachments to user message (EF will handle on save)
                    // We need to attach them to the userMessage object for immediate use
                    // The navigation property will be set when the message is saved; we can also assign them directly
                    // Since the userMessage isn't saved yet, we manually add them to the list:
                    userMessage.Attachments = attachmentEntities;
                }
            }

            // Save user message + attachments (so they get IDs)
            await _unitOfWork.SaveChangesAsync(ct);

            // 4. Build prompt with medical records context if enabled
            string prompt = await BuildPromptAsync(session, content, ct);

            // 5. Prepare images for the AI call (from the newly uploaded files)
            var imageStreams = new List<(Stream Stream, string FileName, string ContentType)>();
            foreach (var att in attachmentEntities)
            {
                var stream = await _fileStorage.DownloadFileAsync(AiChatContainer, att.StoragePath, ct);
                imageStreams.Add((stream, att.FileName, att.ContentType));
            }

            // 6. Call Python inference service with streaming
            var assistantContent = new StringBuilder();
            int? tokensUsed = null;

            try
            {
                using var multipartContent = CreateMultipartRequest(prompt, imageStreams);

                // Create request message
                using var request = new HttpRequestMessage(HttpMethod.Post, "/generate")
                {
                    Content = multipartContent
                };

                // Send with streaming (ResponseHeadersRead is enough)
                using var response = await _aiClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                response.EnsureSuccessStatusCode();

                using var responseStream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(responseStream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Each line is a JSON object: {"token": "Hello"}
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        if (doc.RootElement.TryGetProperty("token", out var tokenProperty))
                        {
                            string token = tokenProperty.GetString() ?? "";
                            assistantContent.Append(token);

                            // Push token to SignalR
                            if (!string.IsNullOrEmpty(connectionId))
                                await _hubDispatcher.SendTokenAsync(connectionId, token, ct);
                        }
                        // Optionally capture tokensUsed if returned
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning("Invalid JSON from AI service: {Line}", line);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI inference failed for session {SessionId}", sessionId);
                if (!string.IsNullOrEmpty(connectionId))
                    await _hubDispatcher.SendErrorAsync(connectionId, "AI service unavailable. Please try again later.", ct);
                return ServiceResult<AIChatMessageDto>.Failure("AI service error", 502);
            }

            // 7. Save assistant message
            var assistantMessage = new AIChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.Assistant,
                Content = assistantContent.ToString(),
                UsedMedicalRecords = session.UseMedicalRecordsContext,
                TokensUsed = tokensUsed,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.AIChatMessages.AddAsync(assistantMessage, ct);

            // Update session timestamp
            session.LastMessageAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);

            // Signal completion
            if (!string.IsNullOrEmpty(connectionId))
                await _hubDispatcher.SendCompletionAsync(connectionId, assistantMessage.Id, ct);

            return ServiceResult<AIChatMessageDto>.Success(MapToMessageDto(assistantMessage));
        }

        public async Task<ServiceResult<List<AIChatMessageDto>>> GetSessionMessagesAsync(
            Guid sessionId, CancellationToken ct)
        {
            var session = await _unitOfWork.AIChatSessions
                .GetByIdWithMessagesAsync(sessionId, ct);
            if (session == null || session.IsDeleted)
                return ServiceResult<List<AIChatMessageDto>>.NotFound("Session not found");

            var messages = session.Messages
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(MapToMessageDto)
                .ToList();

            return ServiceResult<List<AIChatMessageDto>>.Success(messages);
        }

        public async Task<ServiceResult<List<AIChatSessionDto>>> GetPatientSessionsAsync(
            Guid patientId, CancellationToken ct)
        {
            var sessions = await _unitOfWork.AIChatSessions
                .GetByPatientIdAsync(patientId, false, ct);

            var dtos = sessions.Select(s => new AIChatSessionDto
            {
                Id = s.Id,
                PatientId = s.PatientId,
                Title = s.Title,
                UseMedicalRecordsContext = s.UseMedicalRecordsContext,
                CreatedAt = s.CreatedAt,
                LastMessageAt = s.LastMessageAt,
                MessageCount = s.Messages?.Count(m => !m.IsDeleted) ?? 0
            }).ToList();

            return ServiceResult<List<AIChatSessionDto>>.Success(dtos);
        }

        // --- Private helpers ---
        private async Task<string> BuildPromptAsync(AIChatSession session, string userText, CancellationToken ct)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are MedGemma, a helpful medical AI assistant that can analyze medical images and lab reports.");
            sb.AppendLine("Provide clear, accurate explanations in simple language.");
            sb.AppendLine();

            // Add medical record context if enabled
            if (session.UseMedicalRecordsContext)
            {
                var statsResult = await _medicalRecordService.GetStatisticsAsync(session.PatientId, ct);
                if (statsResult.IsSuccess)
                {
                    var stats = statsResult.Data!;
                    sb.AppendLine($"Patient has {stats.TotalRecords} medical records:");
                    sb.AppendLine($"- Lab reports: {stats.LabReports}");
                    sb.AppendLine($"- Imaging: {stats.Imaging}");
                    sb.AppendLine($"- Consultations: {stats.ConsultationNotes}");
                    sb.AppendLine();

                    // Fetch details of 5 most recent records
                    var recordsResult = await _medicalRecordService.GetPatientRecordsAsync(
                        session.PatientId, new MedicalRecordQueryDto { PageSize = 5, SortDescending = true }, ct);
                    if (recordsResult.IsSuccess)
                    {
                        sb.AppendLine("Recent records:");
                        foreach (var rec in recordsResult.Data!.Items)
                        {
                            sb.AppendLine($"- {rec.Title} (Type: {rec.RecordTypeName}): {rec.FindingsSummary ?? "No summary"}");
                        }
                    }
                }
                sb.AppendLine();
            }

            // Include recent chat history (last N messages)
            var recentMessages = await _unitOfWork.AIChatMessages
                .GetBySessionIdAsync(session.Id, false, ct);
            var contextMessages = recentMessages
                .Where(m => !m.IsDeleted)
                .TakeLast(MaxContextMessages)
                .ToList();

            foreach (var msg in contextMessages)
            {
                string role = msg.Role == MessageRole.User ? "User" : "Assistant";
                sb.AppendLine($"{role}: {msg.Content}");
            }

            // Add user's current message
            sb.AppendLine($"User: {userText}");
            sb.AppendLine("Assistant: ");

            return sb.ToString();
        }

        private static MultipartFormDataContent CreateMultipartRequest(
            string prompt, List<(Stream Stream, string FileName, string ContentType)> images)
        {
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(prompt), "prompt");

            foreach (var (stream, fileName, contentType) in images)
            {
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                form.Add(streamContent, "images", fileName);
            }

            return form;
        }

        private static AIChatSessionDto MapToSessionDto(AIChatSession session) => new()
        {
            Id = session.Id,
            PatientId = session.PatientId,
            Title = session.Title,
            UseMedicalRecordsContext = session.UseMedicalRecordsContext,
            CreatedAt = session.CreatedAt,
            LastMessageAt = session.LastMessageAt,
            MessageCount = session.Messages?.Count(m => !m.IsDeleted) ?? 0
        };

        private static AIChatMessageDto MapToMessageDto(AIChatMessage message) => new()
        {
            Id = message.Id,
            SessionId = message.SessionId,
            Role = message.Role == MessageRole.User ? "user" : "assistant",
            Content = message.Content,
            UsedMedicalRecords = message.UsedMedicalRecords,
            TokensUsed = message.TokensUsed,
            CreatedAt = message.CreatedAt,
            Attachments = message.Attachments?.Select(a => new AIChatAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes
            }).ToList() ?? new()
        };
    }
}