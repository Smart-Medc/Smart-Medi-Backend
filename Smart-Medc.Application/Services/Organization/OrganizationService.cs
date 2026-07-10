using AutoMapper;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Organization;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.Services.Organization
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrganizationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<OrganizationSearchResultDto>> SearchAsync(
            OrganizationSearchDto query,
            CancellationToken cancellationToken = default)
        {
            // Validate and sanitize pagination parameters
            if (query.PageNumber < 1)
                query.PageNumber = 1;
            if (query.PageSize < 1)
                query.PageSize = 10;
            if (query.PageSize > 100)
                query.PageSize = 100; // Max page size

            // Validate search term length
            if (!string.IsNullOrWhiteSpace(query.SearchTerm) && query.SearchTerm.Length > 200)
                throw new ArgumentException("Search term is too long (max 200 characters)");

            // Sanitize search term (trim whitespace)
            var sanitizedSearchTerm = query.SearchTerm?.Trim() ?? string.Empty;

            // Search at repository level
            var orgs = await _unitOfWork.Organizations.SearchOrganizationsAsync(
                sanitizedSearchTerm,
                query.City?.Trim(),
                query.Type,
                cancellationToken
            );

            var filteredOrgs = orgs.ToList();

            // Order results (by name)
            var orderedOrgs = filteredOrgs
                .OrderBy(o => o.Name)
                .ToList();

            var totalCount = orderedOrgs.Count;

            // Apply pagination
            var pagedOrgs = orderedOrgs
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var mapped = _mapper.Map<List<OrganizationSearchResultDto>>(pagedOrgs);

            return new PagedResult<OrganizationSearchResultDto>(
                mapped,
                totalCount,
                query.PageNumber,
                query.PageSize
            );
        }

        public async Task<OrganizationDetailDto> GetOrganizationDetailsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            // Validate organization exists
            var org = await _unitOfWork.Organizations.GetByIdWithDetailsAsync(organizationId, cancellationToken);
            if (org == null)
                throw new KeyNotFoundException("Organization not found");

            return _mapper.Map<OrganizationDetailDto>(org);
        }

        public async Task<List<AvailabilityDto>> GetMonthAvailabilityAsync(
            Guid organizationId,
            DateTime month,
            CancellationToken cancellationToken = default)
        {
            // Validate organization exists
            var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Validate month is reasonable
            if (month == default || month.Year < 2020 || month.Year > 2100)
                throw new ArgumentException("Invalid month parameter");

            // Prevent querying too far in the past (e.g., before current month)
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            if (month < currentMonth.AddMonths(-1)) // Allow previous month for edge cases
                throw new ArgumentException("Cannot query availability for past months");

            // Prevent querying too far in the future (e.g., 1 year)
            if (month > currentMonth.AddYears(1))
                throw new ArgumentException("Cannot query availability more than 1 year in advance");

            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var availability = new List<AvailabilityDto>();

            // Get exceptions (holidays) for the range
            var exceptions = await _unitOfWork.OrganizationAvailabilityExceptions
                .GetByDateRangeAsync(organizationId, startDate, endDate, cancellationToken);
            var exceptionsDict = exceptions.ToDictionary(e => e.Date, e => e);

            // Get operating hours (cache for the loop)
            var operatingHours = await _unitOfWork.OrganizationOperatingHours
                .GetByOrganizationIdAsync(organizationId, cancellationToken);
            var hoursDict = operatingHours.ToDictionary(oh => oh.DayOfWeek, oh => oh);

            // Get all appointments for the month in one query (performance optimization)
            var monthAppointments = await _unitOfWork.Appointments.GetByDateRangeAsync(
                organizationId,
                startDate,
                endDate.AddDays(1).AddTicks(-1),
                cancellationToken);

            // Count active appointments per date (exclude cancelled, rejected, no-show)
            var appointmentsByDate = monthAppointments
                .Where(a => a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.Rejected &&
                            a.Status != AppointmentStatus.NoShow)
                .GroupBy(a => a.AppointmentDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // Default slot duration (can be made configurable per organization)
            var slotDuration = TimeSpan.FromMinutes(30);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var status = "Available";
                var dateOnly = DateOnly.FromDateTime(date);

                // Check if closed by exception
                if (exceptionsDict.TryGetValue(dateOnly, out var exception))
                {
                    if (exception.IsFullDayOff)
                    {
                        status = "Closed";
                    }
                    else if (exception.StartTime.HasValue && exception.EndTime.HasValue)
                    {
                        // Partial-day exception — use exception hours for slot counting
                        var openTime = exception.StartTime.Value.ToTimeSpan();
                        var closeTime = exception.EndTime.Value.ToTimeSpan();
                        var totalMinutes = (closeTime - openTime).TotalMinutes;
                        var totalSlots = (int)(totalMinutes / slotDuration.TotalMinutes);
                        var bookedCount = appointmentsByDate
                            .TryGetValue(date.Date, out var c) ? c : 0;

                        status = (totalSlots > 0 && bookedCount >= totalSlots)
                            ? "Fully Booked"
                            : "Available";
                    }
                    // else: malformed exception — treat as available
                }
                // Check regular operating hours
                else if (hoursDict.TryGetValue(date.DayOfWeek, out var hours))
                {
                    if (!hours.IsOpen || hours.OpenTime == null || hours.CloseTime == null)
                    {
                        status = "Closed";
                    }
                    else
                    {
                        // Check if fully booked
                        // Calculate total available slots for this day
                        var openTime = hours.OpenTime.Value.ToTimeSpan();
                        var closeTime = hours.CloseTime.Value.ToTimeSpan();

                        // Calculate how many slots fit in the operating hours
                        var totalMinutes = (closeTime - openTime).TotalMinutes;
                        var totalSlots = (int)(totalMinutes / slotDuration.TotalMinutes);

                        // Get number of booked appointments for this date
                        var bookedCount = appointmentsByDate
                            .TryGetValue(date.Date, out var c) ? c : 0;

                        // If all slots are taken, mark as fully booked
                        if (totalSlots > 0 && bookedCount >= totalSlots)
                            status = "Fully Booked";

                        // Otherwise, it remains "Available"
                    }
                }
                else
                {
                    status = "Closed"; // No hours defined = closed
                }

                availability.Add(new AvailabilityDto { Date = date, Status = status });
            }

            return availability;
        }

        public async Task<List<TimeSlotDto>> GetDailyTimeSlotsAsync(
            Guid organizationId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            // Validate organization exists
            var organization = await _unitOfWork.Organizations
                .GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Validate date is not in the past
            if (date == default || date.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Cannot get time slots for past dates");

            // Validate date is not too far in the future
            if (date.Date > DateTime.UtcNow.Date.AddYears(1))
                throw new ArgumentException(
                    "Cannot get time slots more than 1 year in advance");

            // Check for exception FIRST — it overrides operating hours
            var exception = await _unitOfWork.OrganizationAvailabilityExceptions
                .GetByDateAsync(organizationId, date, cancellationToken);

            // Full day off — no slots
            if (exception != null && exception.IsFullDayOff)
                return new List<TimeSlotDto>();

            // Determine effective open/close times
            TimeOnly effectiveOpen;
            TimeOnly effectiveClose;

            if (exception != null && !exception.IsFullDayOff
                && exception.StartTime.HasValue && exception.EndTime.HasValue)
            {
                // Partial-day exception overrides the weekly template hours
                effectiveOpen = exception.StartTime.Value;
                effectiveClose = exception.EndTime.Value;
            }
            else
            {
                // Fall back to weekly operating hours
                var dayOfWeek = (int)date.DayOfWeek;
                var operatingHours = await _unitOfWork.OrganizationOperatingHours
                    .GetByDayAsync(organizationId, dayOfWeek, cancellationToken);

                if (operatingHours == null || !operatingHours.IsOpen
                    || operatingHours.OpenTime == null
                    || operatingHours.CloseTime == null)
                    return new List<TimeSlotDto>();

                effectiveOpen = operatingHours.OpenTime.Value;
                effectiveClose = operatingHours.CloseTime.Value;
            }

            // Get existing appointments for this date
            var appointments = await _unitOfWork.Appointments.GetByDateRangeAsync(
                organizationId,
                date.Date,
                date.Date.AddDays(1).AddTicks(-1),
                cancellationToken);

            var confirmedAppointments = appointments
                .Where(a => a.Status != AppointmentStatus.Cancelled
                         && a.Status != AppointmentStatus.Rejected
                         && a.Status != AppointmentStatus.NoShow)
                .ToList();

            // Convert TimeOnly to TimeSpan for proper operations
            var slots = new List<TimeSlotDto>();
            var slotDuration = TimeSpan.FromMinutes(30);
            var currentTime = effectiveOpen.ToTimeSpan();
            var endTime = effectiveClose.ToTimeSpan();

            // For today, skip past slots
            if (date.Date == DateTime.UtcNow.Date)
            {
                var now = DateTime.UtcNow.TimeOfDay;
                if (now > currentTime)
                {
                    // Round up to next slot
                    var minutesSinceOpen = (now - currentTime).TotalMinutes;
                    var slotsToSkip = (int)Math.Ceiling(minutesSinceOpen / 30.0);
                    currentTime = currentTime.Add(
                        TimeSpan.FromMinutes(slotsToSkip * 30));
                }
            }

            while (currentTime.Add(slotDuration) <= endTime)
            {
                // Convert TimeSpan back to TimeOnly for comparison with appointments
                var slotEnd = currentTime.Add(slotDuration);
                var slotStartTimeOnly = TimeOnly.FromTimeSpan(currentTime);
                var slotEndTimeOnly = TimeOnly.FromTimeSpan(slotEnd);

                // Check for conflicts
                bool isConflicted = confirmedAppointments.Any(a =>
                    a.StartTime < slotEndTimeOnly
                    && a.EndTime > slotStartTimeOnly);

                slots.Add(new TimeSlotDto
                {
                    StartTime = slotStartTimeOnly,
                    EndTime = slotEndTimeOnly,
                    Status = isConflicted ? "Booked" : "Available"
                });

                currentTime = slotEnd;
            }

            return slots;
        }

        public async Task<List<GetOperatingHoursDto>> GetOperatingHoursAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            // Validate organization exists
            var organization = await _unitOfWork.Organizations.GetByIdAsync(
                organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            var hours = await _unitOfWork.OrganizationOperatingHours
                .GetByOrganizationIdAsync(organizationId, cancellationToken);

            // Return all 7 days — if a day has no record yet, return it as closed
            var allDays = Enum.GetValues<DayOfWeek>();
            var hoursDict = hours.ToDictionary(h => h.DayOfWeek, h => h);

            return allDays.Select(day =>
            {
                if (hoursDict.TryGetValue(day, out var h))
                {
                    return new GetOperatingHoursDto
                    {
                        Day = day.ToString(),
                        IsOpen = h.IsOpen,
                        OpenTime = h.OpenTime.HasValue
                            ? h.OpenTime.Value.ToString("HH:mm")
                            : null,
                        CloseTime = h.CloseTime.HasValue
                            ? h.CloseTime.Value.ToString("HH:mm")
                            : null,
                    };
                }

                // No record exists for this day — treat as closed
                return new GetOperatingHoursDto
                {
                    Day = day.ToString(),
                    IsOpen = false,
                    OpenTime = null,
                    CloseTime = null,
                };
            }).ToList();
        }

        public async Task UpdateOperatingHoursAsync(
            Guid organizationId,
            Guid requestingUserId,
            UpdateOperatingHoursRequestDto request,
            CancellationToken cancellationToken = default)
        {
            // Validate organization exists
            var organization = await _unitOfWork.Organizations.GetByIdAsync(
                organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Verify the requesting user belongs to this organization
            var user = await _unitOfWork.Organizations.GetByUserIdAsync(
                requestingUserId, cancellationToken);
            if (user == null || user.Id != organizationId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to update this organization's schedule");

            foreach (var dto in request.Schedule)
            {
                // Parse the day string to DayOfWeek enum
                if (!Enum.TryParse<DayOfWeek>(dto.Day, ignoreCase: true, out var dayOfWeek))
                    throw new ArgumentException($"Invalid day: {dto.Day}");

                // Validate times when the day is marked as open
                TimeOnly? openTime = null;
                TimeOnly? closeTime = null;

                if (dto.IsOpen)
                {
                    if (string.IsNullOrWhiteSpace(dto.OpenTime) ||
                        string.IsNullOrWhiteSpace(dto.CloseTime))
                        throw new ArgumentException(
                            $"{dto.Day}: OpenTime and CloseTime are required when the day is open");

                    if (!TimeOnly.TryParse(dto.OpenTime, out var parsedOpen))
                        throw new ArgumentException($"{dto.Day}: Invalid OpenTime format");

                    if (!TimeOnly.TryParse(dto.CloseTime, out var parsedClose))
                        throw new ArgumentException($"{dto.Day}: Invalid CloseTime format");

                    if (parsedClose <= parsedOpen)
                        throw new ArgumentException(
                            $"{dto.Day}: CloseTime must be after OpenTime");

                    openTime = parsedOpen;
                    closeTime = parsedClose;
                }

                // Try to find existing record and upsert
                var existing = await _unitOfWork.OrganizationOperatingHours
                    .GetByOrganizationAndDayAsync(organizationId, dayOfWeek, cancellationToken);

                if (existing != null)
                {
                    existing.IsOpen = dto.IsOpen;
                    existing.OpenTime = openTime;
                    existing.CloseTime = closeTime;
                    await _unitOfWork.OrganizationOperatingHours.UpdateAsync(
                        existing, cancellationToken);
                }
                else
                {
                    var newHours = new OrganizationOperatingHours
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        DayOfWeek = dayOfWeek,
                        IsOpen = dto.IsOpen,
                        OpenTime = openTime,
                        CloseTime = closeTime,
                    };
                    await _unitOfWork.OrganizationOperatingHours.AddAsync(
                        newHours, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Add these three methods inside the existing OrganizationService class

        public async Task<AvailabilityExceptionDto?> GetExceptionByDateAsync(
            Guid organizationId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var organization = await _unitOfWork.Organizations
                .GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            var exception = await _unitOfWork.OrganizationAvailabilityExceptions
                .GetByDateAsync(organizationId, date, cancellationToken);

            if (exception == null)
                return null;

            return new AvailabilityExceptionDto
            {
                Id = exception.Id,
                Date = exception.Date.ToString("yyyy-MM-dd"),
                IsFullDayOff = exception.IsFullDayOff,
                StartTime = exception.StartTime?.ToString("HH:mm"),
                EndTime = exception.EndTime?.ToString("HH:mm"),
                Reason = exception.Reason,
            };
        }

        public async Task<AvailabilityExceptionDto> UpsertExceptionAsync(
            Guid organizationId,
            Guid requestingUserId,
            DateTime date,
            UpsertAvailabilityExceptionDto dto,
            CancellationToken cancellationToken = default)
        {
            var organization = await _unitOfWork.Organizations
                .GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Verify ownership
            var user = await _unitOfWork.Organizations
                .GetByUserIdAsync(requestingUserId, cancellationToken);
            if (user == null || user.Id != organizationId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to update this organization's availability");

            // Validate partial-day times when not a full-day-off
            TimeOnly? startTime = null;
            TimeOnly? endTime = null;

            if (!dto.IsFullDayOff)
            {
                if (string.IsNullOrWhiteSpace(dto.StartTime) ||
                    string.IsNullOrWhiteSpace(dto.EndTime))
                    throw new ArgumentException(
                        "StartTime and EndTime are required for partial-day exceptions");

                if (!TimeOnly.TryParse(dto.StartTime, out var parsedStart))
                    throw new ArgumentException("Invalid StartTime format");

                if (!TimeOnly.TryParse(dto.EndTime, out var parsedEnd))
                    throw new ArgumentException("Invalid EndTime format");

                if (parsedEnd <= parsedStart)
                    throw new ArgumentException("EndTime must be after StartTime");

                startTime = parsedStart;
                endTime = parsedEnd;
            }

            // Upsert
            var existing = await _unitOfWork.OrganizationAvailabilityExceptions
                .GetByDateAsync(organizationId, date, cancellationToken);

            OrganizationAvailabilityException entity;

            if (existing != null)
            {
                existing.IsFullDayOff = dto.IsFullDayOff;
                existing.StartTime = startTime;
                existing.EndTime = endTime;
                existing.Reason = dto.Reason;

                await _unitOfWork.OrganizationAvailabilityExceptions
                    .UpdateAsync(existing, cancellationToken);

                entity = existing;
            }
            else
            {
                entity = new OrganizationAvailabilityException
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Date = DateOnly.FromDateTime(date.Date),
                    IsFullDayOff = dto.IsFullDayOff,
                    StartTime = startTime,
                    EndTime = endTime,
                    Reason = dto.Reason,
                    CreatedAt = DateTime.UtcNow,
                };

                await _unitOfWork.OrganizationAvailabilityExceptions
                    .AddAsync(entity, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AvailabilityExceptionDto
            {
                Id = entity.Id,
                Date = entity.Date.ToString("yyyy-MM-dd"),
                IsFullDayOff = entity.IsFullDayOff,
                StartTime = entity.StartTime?.ToString("HH:mm"),
                EndTime = entity.EndTime?.ToString("HH:mm"),
                Reason = entity.Reason,
            };
        }

        public async Task DeleteExceptionAsync(
            Guid organizationId,
            Guid requestingUserId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var organization = await _unitOfWork.Organizations
                .GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Verify ownership
            var user = await _unitOfWork.Organizations
                .GetByUserIdAsync(requestingUserId, cancellationToken);
            if (user == null || user.Id != organizationId)
                throw new UnauthorizedAccessException(
                    "You are not authorized to update this organization's availability");

            var existing = await _unitOfWork.OrganizationAvailabilityExceptions
                .GetByDateAsync(organizationId, date, cancellationToken);

            if (existing == null)
                throw new KeyNotFoundException(
                    "No exception found for this date");

            await _unitOfWork.OrganizationAvailabilityExceptions
                .DeleteAsync(existing.Id, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
