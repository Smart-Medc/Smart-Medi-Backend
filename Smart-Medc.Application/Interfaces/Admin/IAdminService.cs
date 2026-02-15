using Smart_Medc.Application.Common.Results;
using Smart_Medc.Application.DTOs.Admin;

namespace Smart_Medc.Application.Interfaces.Services
{
    public interface IAdminService
    {
        // Dashboard Statistics
        Task<Result<DashboardStatsDto>> GetDashboardStatsAsync();

        // Patient Management
        Task<Result<List<PatientListItemDto>>> GetAllActivePatientsAsync();
        Task<Result<List<PatientListItemDto>>> GetAllDeletedPatientsAsync();
        Task<Result> SoftDeletePatientAsync(Guid patientId, Guid adminUserId);
        Task<Result> RestorePatientAsync(Guid patientId, Guid adminUserId);

        // Organization Management
        Task<Result<List<OrganizationListItemDto>>> GetOrganizationsByStatusAsync(string status);
        Task<Result<OrganizationDetailsDto>> GetOrganizationDetailsAsync(Guid organizationId);
        Task<Result> ApproveOrganizationAsync(ApproveOrganizationRequest request, Guid adminUserId);
        Task<Result> RejectOrganizationAsync(RejectOrganizationRequest request, Guid adminUserId);
        Task<Result> SoftDeleteOrganizationAsync(Guid organizationId, Guid adminUserId);
        Task<Result> RestoreOrganizationAsync(Guid organizationId, Guid adminUserId);
        Task<Result<List<OrganizationListItemDto>>> GetDeletedOrganizationsAsync();

        // Notifications
        Task SendOrganizationRegistrationNotificationAsync(Guid organizationId);
    }
}