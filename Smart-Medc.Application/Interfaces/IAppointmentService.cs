using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Appointment;

namespace Smart_Medc.Application.Interfaces
{
    public interface IAppointmentService
    {
        // Patient Methods
        Task<AppointmentDto> BookAppointmentAsync(
            Guid authenticatedPatientId,
            BookAppointmentDto dto,
            CancellationToken cancellationToken = default);

        Task<PagedResult<AppointmentDto>> GetPatientAppointmentsAsync(
            Guid patientId,
            string? status = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<AppointmentDetailDto> GetAppointmentDetailsAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default);

        Task RescheduleAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            RescheduleAppointmentDto dto,
            CancellationToken cancellationToken = default);

        Task CancelAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CancelAppointmentDto dto,
            CancellationToken cancellationToken = default);

        // Organization Methods
        Task<PagedResult<AppointmentDto>> GetOrganizationAppointmentsAsync(
            Guid organizationId,
            Guid requestingUserId,
            AppointmentQueryDto query,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<PagedResult<AppointmentRequestDto>> GetPendingRequestsAsync(
            Guid organizationId,
            Guid requestingUserId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<AppointmentDto> ConfirmAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            ConfirmAppointmentDto dto,
            CancellationToken cancellationToken = default);

        Task RejectAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            RejectAppointmentDto dto,
            CancellationToken cancellationToken = default);

        Task CompleteAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CompleteAppointmentDto dto,
            CancellationToken cancellationToken = default);

        Task MarkNoShowAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default);
    }
}
