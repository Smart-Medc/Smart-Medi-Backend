using Microsoft.AspNetCore.Http;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Patient;

namespace Smart_Medc.Application.Interfaces
{
    public interface IPatientProfileService
    {
        // Photo section
        Task<ServiceResult<ProfilePhotoResponse>> ChangeProfilePhotoAsync(
            Guid userId,
            IFormFile photoFile,
            CancellationToken cancellationToken = default);
        Task<ServiceResult<ProfilePhotoResponse>> RemoveProfilePhotoAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        // Personal info section
        Task<ServiceResult<PatientProfileDto>> GetPatientProfileAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
        Task<ServiceResult<PatientProfileDto>> UpdatePersonalInfoAsync(
            Guid userId,
            UpdatePersonalInfoRequest request,
            CancellationToken cancellationToken = default);

        // Notification preferences section
        Task<ServiceResult<NotificationPreferencesResponse>> GetNotificationPreferencesAsync(
           Guid userId,
           CancellationToken cancellationToken = default);
        Task<ServiceResult<NotificationPreferenceDto>> UpdateNotificationPreferenceAsync(
            Guid userId,
            UpdateNotificationPreferenceRequest request,
            CancellationToken cancellationToken = default);
        Task<ServiceResult<NotificationPreferencesResponse>> UpdateAllNotificationPreferencesAsync(
            Guid userId,
            UpdateAllNotificationPreferencesRequest request,
            CancellationToken cancellationToken = default);


        // Export data section
        Task<ServiceResult<MedicalRecordsExportDto>> ExportMedicalRecordsAsync(
            Guid userId,
            ExportMedicalRecordsRequest request,
            CancellationToken cancellationToken = default);
        Task<ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>> ExportMedicalRecordsAsFileAsync(
            Guid userId,
            ExportMedicalRecordsRequest request,
            CancellationToken cancellationToken = default);
    }
}