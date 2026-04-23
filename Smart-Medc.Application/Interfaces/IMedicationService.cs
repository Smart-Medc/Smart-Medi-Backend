using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Medications;

namespace Smart_Medc.Application.Interfaces
{
    public interface IMedicationService
    {
        Task<ServiceResult<List<MedicationDto>>> GetMedicationsAsync(Guid patientId, bool includeInactive, CancellationToken ct = default);
        Task<ServiceResult<MedicationDetailDto>> GetMedicationByIdAsync(Guid patientId, Guid medicationId, CancellationToken ct = default);
        Task<ServiceResult<MedicationDto>> CreateMedicationAsync(Guid patientId, CreateMedicationDto dto, CancellationToken ct = default);
        Task<ServiceResult<MedicationDto>> UpdateMedicationAsync(Guid patientId, Guid medicationId, UpdateMedicationDto dto, CancellationToken ct = default);
        Task<ServiceResult> DeleteMedicationAsync(Guid patientId, Guid medicationId, CancellationToken ct = default);
        Task<ServiceResult<ReminderDto>> CreateReminderAsync(Guid patientId, Guid medicationId, CreateReminderDto dto, CancellationToken ct = default);
        Task<ServiceResult<ReminderDto>> UpdateReminderAsync(Guid patientId, Guid medicationId, Guid reminderId, UpdateReminderDto dto, CancellationToken ct = default);
        Task<ServiceResult> DeleteReminderAsync(Guid patientId, Guid medicationId, Guid reminderId, CancellationToken ct = default);
        Task<ServiceResult<List<InteractionDto>>> CheckInteractionsAsync(Guid patientId, CancellationToken ct = default);
    }
}
