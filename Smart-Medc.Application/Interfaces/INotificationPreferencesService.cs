using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Patient;

namespace Smart_Medc.Application.Interfaces
{
    public interface INotificationPreferencesService
    {
        Task<ServiceResult<NotificationPreferencesResponse>> GetAllPreferencesAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
        Task<ServiceResult<NotificationPreferenceDto>> GetPreferenceAsync(
            Guid userId,
            string notificationType,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<NotificationPreferenceDto>> UpdatePreferenceAsync(
            Guid userId,
            UpdateNotificationPreferenceRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<NotificationPreferencesResponse>> UpdateAllPreferencesAsync(
            Guid userId,
            UpdateAllNotificationPreferencesRequest request,
            CancellationToken cancellationToken = default);
        Task<ServiceResult> InitializeDefaultPreferencesAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}