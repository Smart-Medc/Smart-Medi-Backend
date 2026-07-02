using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.DTOs.MedicalRecord;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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
        private const int MaxContextMessages = 6;
        private static readonly TimeSpan AiTimeout = TimeSpan.FromMinutes(4);

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
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = null
            };

            await _unitOfWork.AIChatSessions.AddAsync(session, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<AIChatSessionDto>.Success(MapToSessionDto(session, 0));
        }

        public async Task<ServiceResult<AIChatMessageDto>> SendMessageAsync(
            Guid sessionId,
            string content,
            List<IFormFile>? files,
            string? connectionId,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(content) && (files == null || files.Count == 0))
                return ServiceResult<AIChatMessageDto>.Failure("Message content or file is required.", 400);

            var session = await _unitOfWork.AIChatSessions.GetByIdWithMessagesAsync(sessionId, ct);
            if (session == null || session.IsDeleted)
                return ServiceResult<AIChatMessageDto>.NotFound("Session not found");

            var userMessage = new AIChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.User,
                Content = content ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            var aiStreams = new List<MemoryStream>();
            var attachmentEntities = new List<AIChatMessageAttachment>();
            var assistantContent = new StringBuilder();
            var streamedAnyToken = false;

            MultipartFormDataContent? multipartContent = null;

            try
            {
                await _unitOfWork.AIChatMessages.AddAsync(userMessage, ct);

                multipartContent = new MultipartFormDataContent();

                if (files != null)
                {
                    foreach (var file in files.Where(f => f.Length > 0))
                    {
                        byte[] bytes;
                        await using (var sourceMs = new MemoryStream())
                        {
                            await file.CopyToAsync(sourceMs, ct);
                            bytes = sourceMs.ToArray();
                        }

                        await using (var uploadStream = new MemoryStream(bytes, writable: false))
                        {
                            var uploadResult = await _fileStorage.UploadFileAsync(
                                uploadStream,
                                file.FileName,
                                file.ContentType,
                                AiChatContainer,
                                ct);

                            attachmentEntities.Add(new AIChatMessageAttachment
                            {
                                Id = Guid.NewGuid(),
                                MessageId = userMessage.Id,
                                FileName = file.FileName,
                                StoragePath = uploadResult.StoragePath,
                                ContentType = file.ContentType,
                                FileSizeBytes = uploadResult.FileSizeBytes,
                                UploadedAt = DateTime.UtcNow
                            });
                        }

                        var aiStream = new MemoryStream(bytes, writable: false);
                        aiStreams.Add(aiStream);

                        var streamContent = new StreamContent(aiStream);
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                        multipartContent.Add(streamContent, "images", file.FileName);
                    }
                }

                if (attachmentEntities.Any())
                {
                    await _unitOfWork.AIChatMessageAttachments.AddRangeAsync(attachmentEntities, ct);
                    userMessage.Attachments = attachmentEntities;
                }

                session.LastMessageAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(ct);

                var messages = await BuildMessagesListAsync(session, content, userMessage.Id, ct);
                multipartContent.Add(
                    new StringContent(JsonSerializer.Serialize(messages), Encoding.UTF8, "text/plain"),
                    "messages"
                );

                using var request = new HttpRequestMessage(HttpMethod.Post, "/generate")
                {
                    Content = multipartContent
                };

                using var aiCts = new CancellationTokenSource(AiTimeout);

                using var response = await _aiClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    aiCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync(aiCts.Token);
                    _logger.LogError("AI service error {StatusCode}: {Body}", (int)response.StatusCode, errBody);
                    throw new InvalidOperationException($"AI service returned {(int)response.StatusCode}: {errBody}");
                }

                using var stream = await response.Content.ReadAsStreamAsync(aiCts.Token);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(aiCts.Token);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        using var doc = JsonDocument.Parse(line);

                        if (doc.RootElement.TryGetProperty("error", out var errProp))
                        {
                            var errorText = errProp.GetString() ?? "Unknown AI stream error";
                            throw new InvalidOperationException($"AI stream error: {errorText}");
                        }

                        if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                        {
                            var token = tokenProp.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(token))
                            {
                                assistantContent.Append(token);
                                streamedAnyToken = true;

                                if (!string.IsNullOrEmpty(connectionId))
                                {
                                    try
                                    {
                                        // immediate per-token dispatch to frontend
                                        await _hubDispatcher.SendTokenAsync(connectionId, token, CancellationToken.None);
                                    }
                                    catch (Exception hubEx)
                                    {
                                        _logger.LogWarning(hubEx, "Hub token send failed for session {SessionId}", sessionId);
                                    }
                                }
                            }
                        }

                        if (doc.RootElement.TryGetProperty("done", out var doneProp) &&
                            doneProp.ValueKind == JsonValueKind.True)
                        {
                            break;
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning("Skipping non-JSON AI stream line for session {SessionId}: {Line}", sessionId, line);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "AI request timed out/canceled for session {SessionId}", sessionId);

                if (!streamedAnyToken)
                {
                    await SafeRollbackUserMessageAsync(userMessage);
                    if (!string.IsNullOrEmpty(connectionId))
                        await SafeHubErrorAsync(connectionId, "AI request timed out. Please try again.");
                }

                return ServiceResult<AIChatMessageDto>.Failure("AI request timed out.", 504);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI pipeline runtime failure for session {SessionId}", sessionId);

                if (!streamedAnyToken)
                {
                    await SafeRollbackUserMessageAsync(userMessage);
                    if (!string.IsNullOrEmpty(connectionId))
                        await SafeHubErrorAsync(connectionId, "Medical AI engine is currently unavailable.");
                }

                return ServiceResult<AIChatMessageDto>.Failure($"AI Service Engine Interrupted: {ex.Message}", 502);
            }
            finally
            {
                multipartContent?.Dispose();

                foreach (var s in aiStreams)
                    await s.DisposeAsync();
            }

            if (assistantContent.Length == 0)
            {
                assistantContent.Append("I'm sorry, I couldn't generate a response. Please try again.");

                if (!string.IsNullOrEmpty(connectionId))
                {
                    try
                    {
                        await _hubDispatcher.SendTokenAsync(connectionId, assistantContent.ToString(), CancellationToken.None);
                    }
                    catch { }
                }
            }

            var assistantMessage = new AIChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.Assistant,
                Content = assistantContent.ToString(),
                UsedMedicalRecords = session.UseMedicalRecordsContext,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.AIChatMessages.AddAsync(assistantMessage, ct);
                session.LastMessageAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (OperationCanceledException)
            {
                using var saveCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _unitOfWork.AIChatMessages.AddAsync(assistantMessage, saveCts.Token);
                session.LastMessageAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(saveCts.Token);
            }

            if (!string.IsNullOrEmpty(connectionId))
            {
                try
                {
                    await _hubDispatcher.SendCompletionAsync(connectionId, assistantMessage.Id, CancellationToken.None);
                }
                catch (Exception hubEx)
                {
                    _logger.LogWarning(hubEx, "Hub completion send failed for session {SessionId}", sessionId);
                }
            }

            return ServiceResult<AIChatMessageDto>.Success(MapToMessageDto(assistantMessage));
        }

        public async Task<ServiceResult<List<AIChatMessageDto>>> GetSessionMessagesAsync(
            Guid sessionId, CancellationToken ct)
        {
            var session = await _unitOfWork.AIChatSessions.GetByIdWithMessagesAsync(sessionId, ct);
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
            var sessions = await _unitOfWork.AIChatSessions.GetByPatientIdAsync(patientId, false, ct);

            var dtos = new List<AIChatSessionDto>(sessions.Count);
            foreach (var s in sessions)
            {
                var messageCount = await _unitOfWork.AIChatMessages.CountAsync(
                    m => m.SessionId == s.Id && !m.IsDeleted, ct);

                // keep empty sessions hidden from history, but do not delete here
                if (messageCount == 0) continue;

                dtos.Add(MapToSessionDto(s, messageCount));
            }

            return ServiceResult<List<AIChatSessionDto>>.Success(dtos);
        }

        public async Task<ServiceResult<bool>> DeleteSessionAsync(Guid sessionId, CancellationToken ct)
        {
            var session = await _unitOfWork.AIChatSessions.GetByIdWithMessagesAsync(sessionId, ct);
            if (session == null || session.IsDeleted)
                return ServiceResult<bool>.NotFound("Session not found");

            session.IsDeleted = true;
            session.DeletedAt = DateTime.UtcNow;

            if (session.Messages != null)
            {
                foreach (var msg in session.Messages.Where(m => !m.IsDeleted))
                {
                    msg.IsDeleted = true;
                    msg.DeletedAt = DateTime.UtcNow;
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<bool>.Success(true);
        }

        private async Task SafeRollbackUserMessageAsync(AIChatMessage userMessage)
        {
            try
            {
                await _unitOfWork.AIChatMessages.DeleteAsync(userMessage);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed rolling back user message {MessageId}", userMessage.Id);
            }
        }

        private async Task SafeHubErrorAsync(string connectionId, string message)
        {
            try
            {
                await _hubDispatcher.SendErrorAsync(connectionId, message, CancellationToken.None);
            }
            catch { }
        }

        private async Task<List<Dictionary<string, string>>> BuildMessagesListAsync(
            AIChatSession session, string userText, Guid? excludeMessageId, CancellationToken ct)
        {
            var messages = new List<Dictionary<string, string>>
            {
                new()
                {
                    { "role", "system" },
                    {
                        "content",
                        "You are MedGemma, a helpful medical AI assistant that can analyze medical images and lab reports. Provide clear, accurate explanations in simple language."
                    }
                }
            };

            if (session.UseMedicalRecordsContext)
            {
                var statsResult = await _medicalRecordService.GetStatisticsAsync(session.PatientId, ct);
                if (statsResult.IsSuccess)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Here is a summary of the patient's medical records:");
                    sb.AppendLine($"Total records: {statsResult.Data!.TotalRecords}");
                    sb.AppendLine($"Lab reports: {statsResult.Data.LabReports}, Imaging: {statsResult.Data.Imaging}, Consultations: {statsResult.Data.ConsultationNotes}");

                    var recordsResult = await _medicalRecordService.GetPatientRecordsAsync(
                        session.PatientId,
                        new MedicalRecordQueryDto { PageSize = 3, SortDescending = true },
                        ct);

                    if (recordsResult.IsSuccess)
                    {
                        foreach (var rec in recordsResult.Data!.Items)
                            sb.AppendLine($"- {rec.Title}: {rec.FindingsSummary ?? "No summary"}");
                    }

                    messages.Add(new Dictionary<string, string>
                    {
                        { "role", "system" },
                        { "content", sb.ToString() }
                    });
                }
            }

            var recentMessages = await _unitOfWork.AIChatMessages.GetBySessionIdAsync(session.Id, false, ct);
            foreach (var msg in recentMessages.TakeLast(MaxContextMessages))
            {
                if (excludeMessageId.HasValue && msg.Id == excludeMessageId.Value) continue;

                var role = msg.Role == MessageRole.User ? "user" : "assistant";
                messages.Add(new Dictionary<string, string>
                {
                    { "role", role },
                    { "content", msg.Content }
                });
            }

            messages.Add(new Dictionary<string, string>
            {
                { "role", "user" },
                { "content", userText }
            });

            return messages;
        }

        private static AIChatSessionDto MapToSessionDto(AIChatSession session, int messageCount) => new()
        {
            Id = session.Id,
            PatientId = session.PatientId,
            Title = session.Title,
            UseMedicalRecordsContext = session.UseMedicalRecordsContext,
            CreatedAt = session.CreatedAt,
            LastMessageAt = session.LastMessageAt,
            MessageCount = messageCount
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