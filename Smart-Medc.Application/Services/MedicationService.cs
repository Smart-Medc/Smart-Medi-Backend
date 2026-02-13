using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Medications;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;


namespace Smart_Medc.Application.Services
{
    public class MedicationService : IMedicationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MedicationService> _logger;

        public MedicationService(IUnitOfWork unitOfWork, ILogger<MedicationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResult<List<MedicationDto>>> GetMedicationsAsync(
            Guid patientId, bool includeInactive, CancellationToken ct = default)
        {
            var medications = includeInactive
                ? await _unitOfWork.Medications.GetByPatientIdAsync(patientId, false, ct)
                : await _unitOfWork.Medications.GetActiveMedicationsByPatientIdAsync(patientId, ct);

            var dtos = medications.Select(MapToDto).ToList();
            return ServiceResult<List<MedicationDto>>.Success(dtos);
        }

        public async Task<ServiceResult<MedicationDetailDto>> GetMedicationByIdAsync(
            Guid patientId, Guid medicationId, CancellationToken ct = default)
        {
            var medication = await _unitOfWork.Medications.GetByIdWithRemindersAsync(medicationId, ct);
            if (medication == null || medication.PatientId != patientId || medication.IsDeleted)
                return ServiceResult<MedicationDetailDto>.NotFound("Medication not found");

            return ServiceResult<MedicationDetailDto>.Success(MapToDetailDto(medication));
        }

        public async Task<ServiceResult<MedicationDto>> CreateMedicationAsync(
            Guid patientId, CreateMedicationDto dto, CancellationToken ct = default)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient == null)
                return ServiceResult<MedicationDto>.NotFound("Patient not found");

            var medication = new Medication
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Name = dto.Name,
                Dosage = dto.Dosage,
                Frequency = dto.Frequency,
                Route = dto.Route,
                Instructions = dto.Instructions,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                PrescribingDoctor = dto.PrescribingDoctor,
                Status = MedicationStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Medications.AddAsync(medication, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<MedicationDto>.Success(MapToDto(medication));
        }

        public async Task<ServiceResult<MedicationDto>> UpdateMedicationAsync(
            Guid patientId, Guid medicationId, UpdateMedicationDto dto, CancellationToken ct = default)
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(medicationId, ct);
            if (medication == null || medication.PatientId != patientId || medication.IsDeleted)
                return ServiceResult<MedicationDto>.NotFound("Medication not found");

            medication.Name = dto.Name;
            medication.Dosage = dto.Dosage;
            medication.Frequency = dto.Frequency;
            medication.Route = dto.Route;
            medication.Instructions = dto.Instructions;
            medication.StartDate = dto.StartDate;
            medication.EndDate = dto.EndDate;
            medication.PrescribingDoctor = dto.PrescribingDoctor;
            medication.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Medications.UpdateAsync(medication, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<MedicationDto>.Success(MapToDto(medication));
        }

        public async Task<ServiceResult> DeleteMedicationAsync(
            Guid patientId, Guid medicationId, CancellationToken ct = default)
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(medicationId, ct);
            if (medication == null || medication.PatientId != patientId || medication.IsDeleted)
                return ServiceResult.NotFound("Medication not found");

            medication.IsDeleted = true;
            medication.DeletedAt = DateTime.UtcNow;

            await _unitOfWork.Medications.UpdateAsync(medication, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<ReminderDto>> CreateReminderAsync(
            Guid patientId, Guid medicationId, CreateReminderDto dto, CancellationToken ct = default)
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(medicationId, ct);
            if (medication == null || medication.PatientId != patientId || medication.IsDeleted)
                return ServiceResult<ReminderDto>.NotFound("Medication not found");

            var reminder = new MedicationReminder
            {
                Id = Guid.NewGuid(),
                MedicationId = medicationId,
                ReminderTime = dto.ReminderTime,
                DaysOfWeek = dto.DaysOfWeek,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MedicationReminders.AddAsync(reminder, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<ReminderDto>.Success(new ReminderDto
            {
                Id = reminder.Id,
                ReminderTime = reminder.ReminderTime,
                DaysOfWeek = reminder.DaysOfWeek,
                IsActive = reminder.IsActive
            });
        }

        public async Task<ServiceResult<ReminderDto>> UpdateReminderAsync(
            Guid patientId, Guid medicationId, Guid reminderId, UpdateReminderDto dto, CancellationToken ct = default)
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(medicationId, ct);
            if (medication == null || medication.PatientId != patientId || medication.IsDeleted)
                return ServiceResult<ReminderDto>.NotFound("Medication not found");

            var reminder = await _unitOfWork.MedicationReminders.GetByIdAsync(reminderId, ct);
            if (reminder == null || reminder.MedicationId != medicationId)
                return ServiceResult<ReminderDto>.NotFound("Reminder not found");

            reminder.ReminderTime = dto.ReminderTime;
            reminder.DaysOfWeek = dto.DaysOfWeek;
            reminder.IsActive = dto.IsActive;
            reminder.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.MedicationReminders.UpdateAsync(reminder, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<ReminderDto>.Success(new ReminderDto
            {
                Id = reminder.Id,
                ReminderTime = reminder.ReminderTime,
                DaysOfWeek = reminder.DaysOfWeek,
                IsActive = reminder.IsActive
            });
        }

        public async Task<ServiceResult> DeleteReminderAsync(
            Guid patientId, Guid medicationId, Guid reminderId, CancellationToken ct = default)
        {
            var medication = await _unitOfWork.Medications.GetByIdAsync(medicationId, ct);
            if (medication == null || medication.PatientId != patientId)
                return ServiceResult.NotFound("Medication not found");

            var reminder = await _unitOfWork.MedicationReminders.GetByIdAsync(reminderId, ct);
            if (reminder == null || reminder.MedicationId != medicationId)
                return ServiceResult.NotFound("Reminder not found");

            await _unitOfWork.MedicationReminders.DeleteAsync(reminder, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<List<InteractionDto>>> CheckInteractionsAsync(
            Guid patientId, CancellationToken ct = default)
        {
            var medications = await _unitOfWork.Medications
                .GetMedicationsWithInteractionsAsync(patientId, ct);

            var interactions = medications.Select(m => new InteractionDto
            {
                MedicationId = m.Id,
                MedicationName = m.Name,
                InteractionNotes = m.InteractionNotes
            }).ToList();

            return ServiceResult<List<InteractionDto>>.Success(interactions);
        }

        private static MedicationDto MapToDto(Medication m) => new()
        {
            Id = m.Id,
            Name = m.Name,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            Route = m.Route,
            Instructions = m.Instructions,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            PrescribingDoctor = m.PrescribingDoctor,
            Status = m.Status,
            HasInteraction = m.HasInteraction,
            InteractionNotes = m.InteractionNotes,
            CreatedAt = m.CreatedAt,
            ReminderCount = m.Reminders?.Count(r => r.IsActive) ?? 0,
            AdherencePercentage = CalculateAdherence(m.AdherenceLogs)
        };

        private static MedicationDetailDto MapToDetailDto(Medication m) => new()
        {
            Id = m.Id,
            Name = m.Name,
            Dosage = m.Dosage,
            Frequency = m.Frequency,
            Route = m.Route,
            Instructions = m.Instructions,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            PrescribingDoctor = m.PrescribingDoctor,
            Status = m.Status,
            HasInteraction = m.HasInteraction,
            InteractionNotes = m.InteractionNotes,
            CreatedAt = m.CreatedAt,
            ReminderCount = m.Reminders?.Count(r => r.IsActive) ?? 0,
            AdherencePercentage = CalculateAdherence(m.AdherenceLogs),
            Reminders = m.Reminders?.Select(r => new ReminderDto
            {
                Id = r.Id,
                ReminderTime = r.ReminderTime,
                DaysOfWeek = r.DaysOfWeek,
                IsActive = r.IsActive
            }).ToList() ?? new()
        };

        private static double CalculateAdherence(ICollection<MedicationAdherenceLog>? logs)
        {
            if (logs == null || logs.Count == 0) return 0;
            var taken = logs.Count(l => l.Status == AdherenceStatus.Taken);
            return Math.Round((double)taken / logs.Count * 100, 1);
        }
    }
}
