// Smart_Medc.Application/Services/AI/AIChatService.cs

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.AI;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.AI;
using Smart_Medc.Domain.Interfaces.Repositories;
using System.Text;

namespace Smart_Medc.Application.Services.AI
{
    public class AIChatService : IAIChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<AIChatService> _logger;
        private const string AIChatContainer = "ai-chat-attachments";
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        private static readonly string[] AllowedContentTypes =
        {
            "application/pdf",
            "image/jpeg",
            "image/png",
            "image/jpg",
            "text/plain"
        };

        public AIChatService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            ILogger<AIChatService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════
        // SESSION MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        public async Task<ServiceResult<AIChatSessionDto>> CreateSessionAsync(
            Guid patientId, CreateAIChatSessionDto dto, CancellationToken ct = default)
        {
            try
            {
                // Verify patient exists
                var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
                if (patient == null)
                    return ServiceResult<AIChatSessionDto>.NotFound("Patient not found");

                var session = new AIChatSession
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    Title = dto.Title ?? $"Chat - {DateTime.UtcNow:MMM dd, yyyy}",
                    UseMedicalRecordsContext = dto.UseMedicalRecordsContext,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.AIChatSessions.AddAsync(session, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation($"AI Chat session created: {session.Id}");

                return ServiceResult<AIChatSessionDto>.Success(MapToSessionDto(session));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AI chat session");
                return ServiceResult<AIChatSessionDto>.Failure("Failed to create session");
            }
        }

        public async Task<ServiceResult<List<AIChatSessionListItemDto>>> GetPatientSessionsAsync(
            Guid patientId, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
        {
            try
            {
                var sessions = await _unitOfWork.AIChatSessions.QueryNoTracking()
                    .Where(s => s.PatientId == patientId && !s.IsDeleted)
                    .OrderByDescending(s => s.LastMessageAt ?? s.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(s => s.Messages)
                    .ToListAsync(ct);

                var dtos = sessions.Select(s => new AIChatSessionListItemDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    CreatedAt = s.CreatedAt,
                    LastMessageAt = s.LastMessageAt,
                    MessageCount = s.Messages?.Count ?? 0,
                    LastMessagePreview = s.Messages?
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefault()?
                        .Content
                        .Substring(0, Math.Min(100, s.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Content.Length ?? 0))
                }).ToList();

                return ServiceResult<List<AIChatSessionListItemDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving AI chat sessions");
                return ServiceResult<List<AIChatSessionListItemDto>>.Failure("Failed to retrieve sessions");
            }
        }

        public async Task<ServiceResult<AIChatSessionDetailDto>> GetSessionDetailsAsync(
            Guid patientId, Guid sessionId, CancellationToken ct = default)
        {
            try
            {
                var session = await _unitOfWork.AIChatSessions.QueryNoTracking()
                    .Where(s => s.Id == sessionId && s.PatientId == patientId && !s.IsDeleted)
                    .Include(s => s.Messages)
                    .ThenInclude(m => m.Attachments)
                    .FirstOrDefaultAsync(ct);

                if (session == null)
                    return ServiceResult<AIChatSessionDetailDto>.NotFound("Session not found");

                var dto = new AIChatSessionDetailDto
                {
                    Id = session.Id,
                    Title = session.Title,
                    UseMedicalRecordsContext = session.UseMedicalRecordsContext,
                    CreatedAt = session.CreatedAt,
                    LastMessageAt = session.LastMessageAt,
                    Messages = session.Messages
                        ?.OrderBy(m => m.CreatedAt)
                        .Select(m => MapToMessageDto(m))
                        .ToList() ?? new()
                };

                return ServiceResult<AIChatSessionDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session details");
                return ServiceResult<AIChatSessionDetailDto>.Failure("Failed to retrieve session");
            }
        }

        public async Task<ServiceResult> DeleteSessionAsync(
            Guid patientId, Guid sessionId, CancellationToken ct = default)
        {
            try
            {
                var session = await _unitOfWork.AIChatSessions.GetByIdAsync(sessionId, ct);
                if (session == null || session.PatientId != patientId || session.IsDeleted)
                    return ServiceResult.NotFound("Session not found");

                // Soft delete
                session.IsDeleted = true;
                session.DeletedAt = DateTime.UtcNow;

                await _unitOfWork.AIChatSessions.UpdateAsync(session, ct);

                // Also mark all messages as deleted
                var messages = await _unitOfWork.AIChatMessages.QueryNoTracking()
                    .Where(m => m.SessionId == sessionId)
                    .ToListAsync(ct);

                foreach (var msg in messages)
                {
                    msg.IsDeleted = true;
                    msg.DeletedAt = DateTime.UtcNow;
                    await _unitOfWork.AIChatMessages.UpdateAsync(msg, ct);
                }

                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation($"AI Chat session deleted: {sessionId}");
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session");
                return ServiceResult.Failure("Failed to delete session");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // MESSAGING
        // ═══════════════════════════════════════════════════════════════

        public async Task<ServiceResult<AIChatMessageDto>> InitiateChatStreamAsync(
            Guid patientId, SendAIChatMessageDto dto, CancellationToken ct = default)
        {
            try
            {
                // Verify session exists and belongs to patient
                var session = await _unitOfWork.AIChatSessions.GetByIdAsync(dto.SessionId, ct);
                if (session == null || session.PatientId != patientId || session.IsDeleted)
                    return ServiceResult<AIChatMessageDto>.NotFound("Session not found");

                // Save user message
                var userMessage = new AIChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = dto.SessionId,
                    Role = MessageRole.User,
                    Content = dto.Content,
                    UsedMedicalRecords = dto.UseMedicalRecordsContext,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.AIChatMessages.AddAsync(userMessage, ct);

                // Add attachments to user message
                foreach (var attachmentId in dto.AttachmentIds)
                {
                    var attachment = await _unitOfWork.AIChatMessageAttachments
                        .GetByIdAsync(attachmentId, ct);

                    if (attachment != null && attachment.MessageId == null)
                    {
                        attachment.MessageId = userMessage.Id;
                        await _unitOfWork.AIChatMessageAttachments.UpdateAsync(attachment, ct);
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);

                // Create AI response message (empty, will be filled by streaming)
                var aiMessage = new AIChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = dto.SessionId,
                    Role = MessageRole.Assistant,
                    Content = "", // Will be updated by background job
                    UsedMedicalRecords = dto.UseMedicalRecordsContext,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.AIChatMessages.AddAsync(aiMessage, ct);

                // Update session's last message time
                session.LastMessageAt = DateTime.UtcNow;
                await _unitOfWork.AIChatSessions.UpdateAsync(session, ct);

                await _unitOfWork.SaveChangesAsync(ct);

                // Enqueue background job to process AI response
                // TODO: Hangfire - BackgroundJob.Enqueue(() => _processingJob.ProcessChatMessageAsync(...))

                _logger.LogInformation(
                    $"Chat initiated - User message: {userMessage.Id}, AI response: {aiMessage.Id}");

                return ServiceResult<AIChatMessageDto>.Success(MapToMessageDto(aiMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating chat stream");
                return ServiceResult<AIChatMessageDto>.Failure("Failed to send message");
            }
        }

        public async Task<ServiceResult<AIChatMessageDto>> SendMessageAsync(
            Guid patientId, SendAIChatMessageDto dto, CancellationToken ct = default)
        {
            // This is for non-streaming responses if needed
            return await InitiateChatStreamAsync(patientId, dto, ct);
        }

        /// <summary>
        /// Called by background job after AI has processed the message
        /// </summary>
        public async Task<ServiceResult> SaveAIResponseAsync(
            Guid patientId, Guid sessionId, Guid messageId, string responseContent, CancellationToken ct = default)
        {
            try
            {
                var message = await _unitOfWork.AIChatMessages.GetByIdAsync(messageId, ct);
                if (message == null || message.SessionId != sessionId)
                    return ServiceResult.NotFound("Message not found");

                message.Content = responseContent;
                message.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.AIChatMessages.UpdateAsync(message, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI response");
                return ServiceResult.Failure("Failed to save response");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ATTACHMENTS
        // ═══════════════════════════════════════════════════════════════

        public async Task<ServiceResult<AIChatAttachmentDto>> UploadAttachmentAsync(
            Guid patientId, Guid sessionId, IFormFile file, CancellationToken ct = default)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                    return ServiceResult<AIChatAttachmentDto>.Failure("No file provided");

                if (file.Length > MaxFileSizeBytes)
                    return ServiceResult<AIChatAttachmentDto>.Failure(
                        $"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)}MB");

                if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
                    return ServiceResult<AIChatAttachmentDto>.Failure(
                        "Invalid file type. Allowed: PDF, JPEG, PNG, JPG, TXT");

                // Verify session exists
                var session = await _unitOfWork.AIChatSessions.GetByIdAsync(sessionId, ct);
                if (session == null || session.PatientId != patientId || session.IsDeleted)
                    return ServiceResult<AIChatAttachmentDto>.NotFound("Session not found");

                // Upload to R2
                using var stream = file.OpenReadStream();
                var uploadResult = await _fileStorage.UploadFileAsync(
                    stream, file.FileName, file.ContentType, AIChatContainer, ct);

                // Create attachment record (not yet attached to a message)
                var attachment = new AIChatMessageAttachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = null, // Will be set when message is sent
                    FileName = uploadResult.FileName,
                    StoragePath = uploadResult.StoragePath,
                    ContentType = file.ContentType,
                    FileSizeBytes = uploadResult.FileSizeBytes,
                    UploadedAt = DateTime.UtcNow
                };

                await _unitOfWork.AIChatMessageAttachments.AddAsync(attachment, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                var previewUrl = await _fileStorage.GeneratePresignedUrlAsync(
                    AIChatContainer, uploadResult.StoragePath, TimeSpan.FromHours(24), ct);

                _logger.LogInformation($"Attachment uploaded: {attachment.Id}");

                return ServiceResult<AIChatAttachmentDto>.Success(new AIChatAttachmentDto
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    FileSizeBytes = attachment.FileSizeBytes,
                    UploadedAt = attachment.UploadedAt,
                    PreviewUrl = previewUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment");
                return ServiceResult<AIChatAttachmentDto>.Failure("Failed to upload file");
            }
        }

        public async Task<ServiceResult> DeleteAttachmentAsync(
            Guid patientId, Guid messageId, Guid attachmentId, CancellationToken ct = default)
        {
            try
            {
                var attachment = await _unitOfWork.AIChatMessageAttachments
                    .GetByIdAsync(attachmentId, ct);

                if (attachment == null)
                    return ServiceResult.NotFound("Attachment not found");

                // Verify ownership through message -> session -> patient
                var message = await _unitOfWork.AIChatMessages.GetByIdAsync(messageId, ct);
                if (message == null)
                    return ServiceResult.NotFound("Message not found");

                var session = await _unitOfWork.AIChatSessions.GetByIdAsync(message.SessionId, ct);
                if (session == null || session.PatientId != patientId)
                    return ServiceResult.Failure("Access denied");

                // Delete from R2
                await _fileStorage.DeleteFileAsync(AIChatContainer, attachment.StoragePath, ct);

                // Delete from database
                await _unitOfWork.AIChatMessageAttachments.DeleteAsync(attachment, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation($"Attachment deleted: {attachmentId}");
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment");
                return ServiceResult.Failure("Failed to delete attachment");
            }
        }

        public async Task<ServiceResult<List<MedicalRecordAttachmentDto>>> GetAvailableAttachmentsAsync(
            Guid patientId, CancellationToken ct = default)
        {
            try
            {
                // Get all non-deleted medical records for patient
                var records = await _unitOfWork.MedicalRecords.QueryNoTracking()
                    .Where(r => r.PatientId == patientId && !r.IsDeleted)
                    .Include(r => r.Documents.Where(d => !d.IsDeleted))
                    .ToListAsync(ct);

                var attachments = new List<MedicalRecordAttachmentDto>();

                foreach (var record in records)
                {
                    foreach (var doc in record.Documents)
                    {
                        attachments.Add(new MedicalRecordAttachmentDto
                        {
                            DocumentId = doc.Id.ToString(),
                            FileName = doc.OriginalFileName,
                            StoragePath = doc.StoragePath,
                            ContentType = doc.ContentType,
                            RecordTitle = record.Title,
                            UploadedAt = doc.UploadedAt
                        });
                    }
                }

                return ServiceResult<List<MedicalRecordAttachmentDto>>.Success(attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available attachments");
                return ServiceResult<List<MedicalRecordAttachmentDto>>.Failure(
                    "Failed to retrieve attachments");
            }
        }

        public async Task<ServiceResult<Stream>> GetAttachmentStreamAsync(
            Guid patientId, string storagePath, CancellationToken ct = default)
        {
            try
            {
                // Verify the attachment belongs to patient's session
                var attachment = await _unitOfWork.AIChatMessageAttachments.QueryNoTracking()
                    .Where(a => a.StoragePath == storagePath)
                    .Include(a => a.Message)
                    .ThenInclude(m => m!.Session)
                    .FirstOrDefaultAsync(ct);

                if (attachment?.Message?.Session?.PatientId != patientId)
                    return ServiceResult<Stream>.Failure("Access denied");

                var stream = await _fileStorage.DownloadFileAsync(
                    AIChatContainer, storagePath, ct);

                return ServiceResult<Stream>.Success(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attachment stream");
                return ServiceResult<Stream>.Failure("Failed to retrieve attachment");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // MAPPING HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static AIChatSessionDto MapToSessionDto(AIChatSession session) => new()
        {
            Id = session.Id,
            Title = session.Title,
            UseMedicalRecordsContext = session.UseMedicalRecordsContext,
            CreatedAt = session.CreatedAt,
            LastMessageAt = session.LastMessageAt,
            MessageCount = session.Messages?.Count ?? 0
        };

        private static AIChatMessageDto MapToMessageDto(AIChatMessage message) => new()
        {
            Id = message.Id,
            Role = message.Role == MessageRole.User ? "user" : "assistant",
            Content = message.Content,
            UsedMedicalRecords = message.UsedMedicalRecords,
            CreatedAt = message.CreatedAt,
            Attachments = message.Attachments?
                .Select(a => new AIChatAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes,
                    UploadedAt = a.UploadedAt
                })
                .ToList() ?? new()
        };
    }
}