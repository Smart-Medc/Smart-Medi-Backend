using Microsoft.AspNetCore.Http;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.Interfaces
{
    public interface IAIChatService
    {
        // Session Management
        Task<ServiceResult<AIChatSessionDto>> CreateSessionAsync(
            Guid patientId, CreateAIChatSessionDto dto, CancellationToken ct = default);

        Task<ServiceResult<List<AIChatSessionListItemDto>>> GetPatientSessionsAsync(
            Guid patientId, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default);

        Task<ServiceResult<AIChatSessionDetailDto>> GetSessionDetailsAsync(
            Guid patientId, Guid sessionId, CancellationToken ct = default);

        Task<ServiceResult> DeleteSessionAsync(
            Guid patientId, Guid sessionId, CancellationToken ct = default);

        // Message & Chat
        Task<ServiceResult<AIChatMessageDto>> SendMessageAsync(
            Guid patientId, SendAIChatMessageDto dto, CancellationToken ct = default);

        // Streaming via SignalR - returns the message ID so client can listen to hub
        Task<ServiceResult<AIChatMessageDto>> InitiateChatStreamAsync(
            Guid patientId, SendAIChatMessageDto dto, CancellationToken ct = default);

        // Attachment Management
        Task<ServiceResult<AIChatAttachmentDto>> UploadAttachmentAsync(
            Guid patientId, Guid sessionId, IFormFile file, CancellationToken ct = default);

        Task<ServiceResult> DeleteAttachmentAsync(
            Guid patientId, Guid messageId, Guid attachmentId, CancellationToken ct = default);

        // Get available medical records for attachment selection
        Task<ServiceResult<List<MedicalRecordAttachmentDto>>> GetAvailableAttachmentsAsync(
            Guid patientId, CancellationToken ct = default);

        // Get file stream from R2 (for analysis)
        Task<ServiceResult<Stream>> GetAttachmentStreamAsync(
            Guid patientId, string storagePath, CancellationToken ct = default);
    }
}
