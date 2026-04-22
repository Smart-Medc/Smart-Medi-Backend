using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Appointment;
using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Entities.AppointmentModels;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.Services.Organization
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IDataSharingService _dataSharingService;
        private readonly INotificationService _notificationService;

        public AppointmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IDataSharingService dataSharingService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _dataSharingService = dataSharingService;
            _notificationService = notificationService;
        }

        // =========================
        // Patient Methods
        // =========================

        public async Task<AppointmentDto> BookAppointmentAsync(
            Guid userId,
            BookAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
            if (patient == null)
                throw new UnauthorizedAccessException("Patient not found");

            // Validate organization exists
            var organization = await _unitOfWork.Organizations.GetByIdAsync(dto.OrganizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Validate doctor if provided
            if (dto.DoctorId.HasValue)
            {
                var doctor = await _unitOfWork.Doctors.GetByIdAsync(dto.DoctorId.Value, cancellationToken);
                if (doctor == null || doctor.OrganizationId != dto.OrganizationId)
                    throw new InvalidOperationException("Doctor not found or doesn't belong to this organization");
            }

            // Parse and validate visit type
            if (!Enum.TryParse<AppointmentType>(dto.VisitType, true, out var visitType))
                throw new ArgumentException("Invalid visit type");

            // Calculate duration and end time
            var durationMinutes = 30; // Default, or fetch from organization settings
            var endTime = dto.StartTime.AddMinutes(durationMinutes);

            // Check for conflicts
            var conflict = await _unitOfWork.Appointments.HasConflictingAppointmentAsync(
                dto.OrganizationId,
                dto.Date,
                dto.StartTime.ToTimeSpan(),
                endTime.ToTimeSpan(),
                cancellationToken: cancellationToken
            );

            if (conflict)
                throw new InvalidOperationException("The selected time slot is not available");

            Guid? shareCodeId = null;

            var strategy = _unitOfWork.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                // Begin transaction for multi-entity operation
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (dto.ShareRecords && dto.RecordsToShare.Any())
                    {
                        // Generate share code
                        var codeDto = await _dataSharingService.GenerateShareCodeAsync(
                            userId,
                            new DTOs.DataSharing.GenerateShareCodeDto
                            {
                                ExpirationType = "ThirtyDays",
                                SpecificRecordIds = dto.RecordsToShare
                            },
                            cancellationToken);

                        shareCodeId = codeDto.Id;
                    }

                    // Generate unique appointment number
                    var appointmentNumber = await GenerateUniqueAppointmentNumberAsync(cancellationToken);

                    var appointment = new Appointment
                    {
                        Id = Guid.NewGuid(),
                        AppointmentNumber = appointmentNumber,
                        PatientId = patient.Id,
                        OrganizationId = dto.OrganizationId,
                        DoctorId = dto.DoctorId,
                        AppointmentDate = dto.Date.Date,
                        StartTime = dto.StartTime,
                        EndTime = endTime,
                        DurationMinutes = durationMinutes,
                        Type = visitType,
                        ReasonForVisit = dto.Reason,
                        Status = AppointmentStatus.Pending,
                        IsRecordsShared = dto.ShareRecords,
                        DataShareCodeId = shareCodeId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Appointments.AddAsync(appointment, cancellationToken);

                    // Add status history
                    await _unitOfWork.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        AppointmentId = appointment.Id,
                        FromStatus = AppointmentStatus.Pending,
                        ToStatus = AppointmentStatus.Pending,
                        Reason = "Appointment created",
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    // Notify Organization about the new appointment request
                    await _notificationService.NotifyAppointmentStatusChangedAsync(
                        appointment.Id, AppointmentStatus.Pending, null, cancellationToken);

                    // Reload with navigation properties for mapping
                    appointment = await _unitOfWork.Appointments.GetByIdAsync(appointment.Id, cancellationToken);

                    return _mapper.Map<AppointmentDto>(appointment);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            });
        }

        public async Task<PagedResult<AppointmentDto>> GetPatientAppointmentsAsync(
            Guid patientId,
            Guid authenticatedUserId,
            string? status = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, cancellationToken);
            if (patient == null || patient.UserId != authenticatedUserId)
                throw new UnauthorizedAccessException("You are not authorized to access this patient's appointments");

            // Validate pagination parameters
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 20;
            if (pageSize > 100)
                pageSize = 100; // Max page size

            IEnumerable<Appointment> appointments;

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
            {
                var allAppointments = await _unitOfWork.Appointments.GetByPatientIdAsync(patientId, cancellationToken);
                appointments = allAppointments.Where(a => a.Status == statusEnum);
            }
            else
            {
                appointments = await _unitOfWork.Appointments.GetByPatientIdAsync(patientId, cancellationToken);
            }

            // Order results (most recent first)
            var orderedAppointments = appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToList();

            var totalCount = orderedAppointments.Count;

            // Apply pagination
            var pagedAppointments = orderedAppointments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var mappedAppointments = _mapper.Map<List<AppointmentDto>>(pagedAppointments);

            return new PagedResult<AppointmentDto>(
                mappedAppointments,
                totalCount,
                pageNumber,
                pageSize
            );
        }

        public async Task<AppointmentDetailDto> GetAppointmentDetailsAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {

            // Authorization check - must be patient or organization owner
            var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            // Appointment Retreival 
            var appointment = await _unitOfWork.Appointments.GetByIdWithDetailsAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Authorization check - must have a permission to retreive appointment
            bool isAuthorized = false;
            if (user.UserType == UserType.Patient)
            {
                var patient = await _unitOfWork.Patients.GetByUserIdAsync(requestingUserId, cancellationToken);
                isAuthorized = patient?.Id == appointment.PatientId;
            }
            else if (user.UserType == UserType.Organization)
            {
                var organization = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
                isAuthorized = organization?.Id == appointment.OrganizationId;
            }

            if (!isAuthorized)
                throw new UnauthorizedAccessException("You don't have permission to view this appointment");

            return _mapper.Map<AppointmentDetailDto>(appointment);
        }

        public async Task RescheduleAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            RescheduleAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            // Retreive Appointment
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Validate status
            if (appointment.Status == AppointmentStatus.Completed ||
                appointment.Status == AppointmentStatus.Cancelled)
                throw new InvalidOperationException("Cannot reschedule completed or cancelled appointments");

            // Authorization check
            var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            // Verify Patient User (Users Only Can Reschedule)
            bool isPatient = false;
            if (user.UserType == UserType.Patient)
            {
                var patient = await _unitOfWork.Patients.GetByUserIdAsync(requestingUserId, cancellationToken);
                isPatient = patient?.Id == appointment.PatientId;
                if (!isPatient)
                    throw new UnauthorizedAccessException("You don't have permission to reschedule this appointment");
            }
            else
            {
                throw new UnauthorizedAccessException("Only patients can reschedule appointments");
            }

            // Check organization reschedule limit
            var organization = await _unitOfWork.Organizations.GetByIdAsync(appointment.OrganizationId, cancellationToken);
            if (appointment.RescheduleCount >= organization?.MaxReschedulesAllowed)
                throw new InvalidOperationException($"Maximum reschedule limit ({organization.MaxReschedulesAllowed}) reached");

            // Check availability for new slot
            var durationMinutes = appointment.DurationMinutes > 0 ? appointment.DurationMinutes : 30; // fixed 30 (can be replaced with organization policy)
            var newEndTime = dto.NewStartTime.AddMinutes(durationMinutes);

            var conflict = await _unitOfWork.Appointments.HasConflictingAppointmentAsync(
                appointment.OrganizationId,
                dto.NewDate,
                dto.NewStartTime.ToTimeSpan(),
                newEndTime.ToTimeSpan(),
                excludeAppointmentId: appointmentId,
                cancellationToken: cancellationToken
            );

            if (conflict)
                throw new InvalidOperationException("The new time slot is not available");

            var oldStatus = appointment.Status;

            appointment.AppointmentDate = dto.NewDate.Date;
            appointment.StartTime = dto.NewStartTime;
            appointment.EndTime = newEndTime;
            appointment.RescheduleCount++;
            appointment.Status = AppointmentStatus.Pending; // Reset to pending for org approval
            appointment.UpdatedAt = DateTime.UtcNow;

            // Add status history with correct old status
            await _unitOfWork.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                FromStatus = oldStatus,
                ToStatus = AppointmentStatus.Pending,
                Reason = $"Rescheduled by patient. {dto.Reason}",
                ChangedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.Appointments.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify organization about reschedule
            await _notificationService.SendToOrganizationAsync(new CreateOrganizationNotificationDto
            {
                OrganizationId = appointment.OrganizationId,
                Type = OrganizationNotificationType.AppointmentRescheduled,
                Priority = NotificationPriority.Normal,
                Title = "Appointment Rescheduled",
                Message = $"A patient rescheduled their appointment to {dto.NewDate:MMM dd, yyyy} at {dto.NewStartTime:hh\\:mm tt}.",
                ActionUrl = $"/org/appointments/{appointmentId}",
                Data = $"{{\"appointmentId\":\"{appointmentId}\"}}"
            }, cancellationToken);
        }

        public async Task CancelAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CancelAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            // Retreive Appointment
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Validate status
            if (appointment.Status == AppointmentStatus.Completed ||
                appointment.Status == AppointmentStatus.Cancelled)
                throw new InvalidOperationException("Cannot cancel completed or already cancelled appointments");

            // Authorization - only patient or organization can cancel
            var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            bool isAuthorized = false;
            if (user.UserType == UserType.Patient)
            {
                var patient = await _unitOfWork.Patients.GetByUserIdAsync(requestingUserId, cancellationToken);
                isAuthorized = patient?.Id == appointment.PatientId;
            }
            else if (user.UserType == UserType.Organization)
            {
                var organization = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
                isAuthorized = organization?.Id == appointment.OrganizationId;
            }

            if (!isAuthorized)
                throw new UnauthorizedAccessException("You don't have permission to cancel this appointment");

            var oldStatus = appointment.Status;
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = dto.Reason;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancelledByUserId = requestingUserId;
            appointment.UpdatedAt = DateTime.UtcNow;

            // Add status history
            await _unitOfWork.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                FromStatus = oldStatus,
                ToStatus = AppointmentStatus.Cancelled,
                Reason = dto.Reason,
                ChangedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.Appointments.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify the OTHER side — whoever didn't cancel gets notified
            if (user.UserType == UserType.Patient)
            {
                await _notificationService.SendToOrganizationAsync(new CreateOrganizationNotificationDto
                {
                    OrganizationId = appointment.OrganizationId,
                    Type = OrganizationNotificationType.AppointmentCancelled,
                    Priority = NotificationPriority.Normal,
                    Title = "Appointment Cancelled by Patient",
                    Message = $"A patient cancelled their appointment. Reason: {dto.Reason}",
                    ActionUrl = $"/org/appointments/{appointmentId}",
                    Data = $"{{\"appointmentId\":\"{appointmentId}\"}}"
                }, cancellationToken);
            }
            else if (user.UserType == UserType.Organization)
            {
                await _notificationService.SendToPatientAsync(new CreatePatientNotificationDto
                {
                    PatientId = appointment.PatientId,
                    Type = PatientNotificationType.AppointmentCancellation,
                    Priority = NotificationPriority.High,
                    Title = "Appointment Cancelled",
                    Message = $"Your appointment has been cancelled by the provider. Reason: {dto.Reason}",
                    ActionUrl = $"/appointments/{appointmentId}",
                    Data = $"{{\"appointmentId\":\"{appointmentId}\"}}"
                }, cancellationToken);
            }
        }

        // =========================
        // Organization Methods
        // =========================

        public async Task<PagedResult<AppointmentDto>> GetOrganizationAppointmentsAsync(
            Guid organizationId,
            Guid requestingUserId,
            AppointmentQueryDto query,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Validate organization exists
            var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Authorization check - must be the organization owner
            var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            bool isAuthorized = false;
            if (user.UserType == UserType.Organization)
            {
                var requestingOrg = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
                isAuthorized = requestingOrg?.Id == organizationId;
            }

            if (!isAuthorized)
                throw new UnauthorizedAccessException("You don't have permission to view these appointments");

            // Validate date range if both provided
            if (query.StartDate.HasValue && query.EndDate.HasValue && query.StartDate > query.EndDate)
                throw new ArgumentException("StartDate cannot be greater than EndDate");

            // Validate pagination parameters
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 20;
            if (pageSize > 100)
                pageSize = 100; // Max page size

            // Get appointments from repository
            var appointments = await _unitOfWork.Appointments.GetByOrganizationIdAsync(organizationId, cancellationToken);

            // Apply filters in-memory
            IEnumerable<Appointment> filteredAppointments = appointments;

            if (query.StartDate.HasValue)
                filteredAppointments = filteredAppointments.Where(a => a.AppointmentDate >= query.StartDate.Value.Date);

            if (query.EndDate.HasValue)
                filteredAppointments = filteredAppointments.Where(a => a.AppointmentDate <= query.EndDate.Value.Date);

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                if (!Enum.TryParse<AppointmentStatus>(query.Status, true, out var statusEnum))
                    throw new ArgumentException($"Invalid status value: {query.Status}");

                filteredAppointments = filteredAppointments.Where(a => a.Status == statusEnum);
            }

            // Order results (most recent first)
            var orderedAppointments = filteredAppointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToList();

            var totalCount = orderedAppointments.Count;

            // Apply pagination
            var pagedAppointments = orderedAppointments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var mappedAppointments = _mapper.Map<List<AppointmentDto>>(pagedAppointments);

            return new PagedResult<AppointmentDto>(
                mappedAppointments,
                totalCount,
                pageNumber,
                pageSize
            );
        }

        public async Task<PagedResult<AppointmentRequestDto>> GetPendingRequestsAsync(
            Guid organizationId,
            Guid requestingUserId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Validate organization exists
            var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Authorization check - must be the organization owner
            var user = await _unitOfWork.Users.GetByIdAsync(requestingUserId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            bool isAuthorized = false;
            if (user.UserType == UserType.Organization)
            {
                var requestingOrg = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
                isAuthorized = requestingOrg?.Id == organizationId;
            }

            if (!isAuthorized)
                throw new UnauthorizedAccessException("You don't have permission to view these appointment requests");

            // Validate pagination parameters
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 20;
            if (pageSize > 100)
                pageSize = 100; // Max page size

            // Get all appointments for organization
            var appointments = await _unitOfWork.Appointments.GetByOrganizationIdAsync(organizationId, cancellationToken);

            // Filter to pending status and order by date/time (earliest first for requests)
            var pendingAppointments = appointments
                .Where(a => a.Status == AppointmentStatus.Pending)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToList();

            var totalCount = pendingAppointments.Count;

            // Apply pagination
            var pagedAppointments = pendingAppointments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var mappedRequests = _mapper.Map<List<AppointmentRequestDto>>(pagedAppointments);

            return new PagedResult<AppointmentRequestDto>(
                mappedRequests,
                totalCount,
                pageNumber,
                pageSize
            );
        }

        public async Task<AppointmentDto> ConfirmAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            ConfirmAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Authorization check
            var organization = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
            if (organization == null || organization.Id != appointment.OrganizationId)
                throw new UnauthorizedAccessException("You don't have permission to confirm this appointment");

            // Validate status
            if (appointment.Status != AppointmentStatus.Pending)
                throw new InvalidOperationException("Only pending appointments can be confirmed");

            var oldStatus = appointment.Status;
            appointment.Status = AppointmentStatus.Confirmed;
            appointment.PreparationInstructions = dto.PreparationInstructions;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                FromStatus = oldStatus,
                ToStatus = AppointmentStatus.Confirmed,
                ChangedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.Appointments.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify patient about confirmation
            await _notificationService.SendToPatientAsync(new CreatePatientNotificationDto
            {
                PatientId = appointment.PatientId,
                Type = PatientNotificationType.AppointmentConfirmation,
                Priority = NotificationPriority.High,
                Title = "Appointment Confirmed",
                Message = $"Your appointment on {appointment.AppointmentDate:MMM dd, yyyy} at {appointment.StartTime:hh\\:mm tt} has been confirmed." +
              (string.IsNullOrEmpty(dto.PreparationInstructions) ? "" : $" Preparation: {dto.PreparationInstructions}"),
                ActionUrl = $"/appointments/{appointmentId}",
                Data = $"{{\"appointmentId\":\"{appointmentId}\"}}"
            }, cancellationToken);

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task RejectAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            RejectAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Authorization check
            var organization = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
            if (organization == null || organization.Id != appointment.OrganizationId)
                throw new UnauthorizedAccessException("You don't have permission to reject this appointment");

            // Validate status
            if (appointment.Status != AppointmentStatus.Pending)
                throw new InvalidOperationException("Only pending appointments can be rejected");

            var oldStatus = appointment.Status;
            appointment.Status = AppointmentStatus.Rejected;
            appointment.CancellationReason = dto.Reason;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancelledByUserId = requestingUserId;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                FromStatus = oldStatus,
                ToStatus = AppointmentStatus.Rejected,
                Reason = dto.Reason,
                ChangedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.Appointments.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify patient about rejection
            await _notificationService.SendToPatientAsync(new CreatePatientNotificationDto
            {
                PatientId = appointment.PatientId,
                Type = PatientNotificationType.AppointmentCancellation,
                Priority = NotificationPriority.High,
                Title = "Appointment Request Rejected",
                Message = $"Your appointment request was rejected. Reason: {dto.Reason}",
                ActionUrl = $"/appointments/{appointmentId}",
                Data = $"{{\"appointmentId\":\"{appointmentId}\"}}"
            }, cancellationToken);
        }

        public async Task CompleteAppointmentAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CompleteAppointmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Authorization check
            var organization = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
            if (organization == null || organization.Id != appointment.OrganizationId)
                throw new UnauthorizedAccessException("You don't have permission to complete this appointment");

            // Validate status
            if (appointment.Status != AppointmentStatus.Confirmed &&
                appointment.Status != AppointmentStatus.InProgress)
                throw new InvalidOperationException("Only confirmed or in-progress appointments can be completed");

            var oldStatus = appointment.Status;
            appointment.Status = AppointmentStatus.Completed;
            appointment.CompletedAt = DateTime.UtcNow;
            appointment.CompletionNotes = dto.Notes;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                FromStatus = oldStatus,
                ToStatus = AppointmentStatus.Completed,
                ChangedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.Appointments.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify patient about completion
            await _notificationService.SendToPatientAsync(new CreatePatientNotificationDto
            {
                PatientId = appointment.PatientId,
                Type = PatientNotificationType.SystemNotification,
                Priority = NotificationPriority.Low,
                Title = "Appointment Completed",
                Message = $"Your appointment on {appointment.AppointmentDate:MMM dd, yyyy} has been marked as completed.",
                ActionUrl = $"/appointments/{appointmentId}",
                Data = $"{{\"appointmentId\":\"{appointmentId}\"}}"
            }, cancellationToken);
        }

        public async Task MarkNoShowAsync(
            Guid appointmentId,
            Guid requestingUserId,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Authorization check
            var organization = await _unitOfWork.Organizations.GetByUserIdAsync(requestingUserId, cancellationToken);
            if (organization == null || organization.Id != appointment.OrganizationId)
                throw new UnauthorizedAccessException("You don't have permission to mark this appointment");

            // Validate status
            if (appointment.Status != AppointmentStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed appointments can be marked as no-show");

            var oldStatus = appointment.Status;
            appointment.Status = AppointmentStatus.NoShow;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.AppointmentStatusHistories.AddAsync(new AppointmentStatusHistory
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                FromStatus = oldStatus,
                ToStatus = AppointmentStatus.NoShow,
                Reason = "Patient did not attend",
                ChangedByUserId = requestingUserId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            await _unitOfWork.Appointments.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify patient about no-show
            await _notificationService.SendToPatientAsync(new CreatePatientNotificationDto
            {
                PatientId = appointment.PatientId,
                Type = PatientNotificationType.SystemNotification,
                Priority = NotificationPriority.Normal,
                Title = "Missed Appointment",
                Message = $"You were marked as a no-show for your appointment on {appointment.AppointmentDate:MMM dd, yyyy} at {appointment.StartTime:hh\\:mm tt}.",
                ActionUrl = $"/appointments/{appointmentId}",
                Data = $"{{\"appointmentId\":\"{appointmentId}\"}}"
            }, cancellationToken);
        }

        public async Task SaveNotesAsync(
            Guid appointmentId,
            Guid requestingUserId,
            SaveNotesDto dto,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _unitOfWork.Appointments
                .GetByIdAsync(appointmentId, cancellationToken);

            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            // Only the organization that owns the appointment can save notes
            var organization = await _unitOfWork.Organizations
                .GetByUserIdAsync(requestingUserId, cancellationToken);

            if (organization == null || organization.Id != appointment.OrganizationId)
                throw new UnauthorizedAccessException(
                    "You don't have permission to save notes for this appointment");

            appointment.CompletionNotes = dto.Notes;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Appointments.UpdateAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Helper method for unique appointment number generation
        private async Task<string> GenerateUniqueAppointmentNumberAsync(CancellationToken cancellationToken)
        {
            string appointmentNumber;
            bool isUnique;

            do
            {
                // Format: APT-YYYYMMDD-RANDOM6
                var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
                var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
                appointmentNumber = $"APT-{datePart}-{randomPart}";

                isUnique = await _unitOfWork.Appointments.GetByAppointmentNumberAsync(appointmentNumber, cancellationToken) == null;
            } while (!isUnique);

            return appointmentNumber;
        }
    }
}
