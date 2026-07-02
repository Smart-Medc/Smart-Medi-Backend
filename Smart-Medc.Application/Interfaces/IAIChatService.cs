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
        Task<ServiceResult<AIChatSessionDto>> CreateSessionAsync(
            Guid patientId, CreateSessionRequestDto dto, CancellationToken ct = default);

        Task<ServiceResult<AIChatMessageDto>> SendMessageAsync(
            Guid sessionId, string content, List<IFormFile>? files,
            string? connectionId, CancellationToken ct = default);

        Task<ServiceResult<List<AIChatMessageDto>>> GetSessionMessagesAsync(
            Guid sessionId, CancellationToken ct = default);

        Task<ServiceResult<List<AIChatSessionDto>>> GetPatientSessionsAsync(
            Guid patientId, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteSessionAsync(Guid sessionId, CancellationToken ct);
    }
}
