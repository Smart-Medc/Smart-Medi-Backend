using AutoMapper;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Organization;
using Smart_Medc.Application.Interfaces;
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
                if (exceptionsDict.TryGetValue(dateOnly, out var exception) && exception.IsFullDayOff)
                {
                    status = "Closed";
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
                        var bookedCount = appointmentsByDate.TryGetValue(date.Date, out var count) ? count : 0;

                        // If all slots are taken, mark as fully booked
                        if (totalSlots > 0 && bookedCount >= totalSlots)
                        {
                            status = "Fully Booked";
                        }

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
            var organization = await _unitOfWork.Organizations.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException("Organization not found");

            // Validate date is not in the past
            if (date == default || date.Date < DateTime.UtcNow.Date)
                throw new ArgumentException("Cannot get time slots for past dates");

            // Validate date is not too far in the future
            if (date.Date > DateTime.UtcNow.Date.AddYears(1))
                throw new ArgumentException("Cannot get time slots more than 1 year in advance");

            var dayOfWeek = (int)date.DayOfWeek;

            // Get operating hours for the day
            var operatingHours = await _unitOfWork.OrganizationOperatingHours
                .GetByDayAsync(organizationId, dayOfWeek, cancellationToken);

            if (operatingHours == null || !operatingHours.IsOpen ||
                operatingHours.OpenTime == null || operatingHours.CloseTime == null)
                return new List<TimeSlotDto>();

            // Check for exception on this date
            var exception = await _unitOfWork.OrganizationAvailabilityExceptions
                .GetByDateAsync(organizationId, date, cancellationToken);

            if (exception != null && exception.IsFullDayOff)
                return new List<TimeSlotDto>();

            // Get existing appointments for this date
            var appointments = await _unitOfWork.Appointments.GetByDateRangeAsync(
                organizationId,
                date.Date,
                date.Date.AddDays(1).AddTicks(-1),
                cancellationToken);

            var confirmedAppointments = appointments
                .Where(a => a.Status != AppointmentStatus.Cancelled &&
                           a.Status != AppointmentStatus.Rejected &&
                           a.Status != AppointmentStatus.NoShow)
                .ToList();

            var slots = new List<TimeSlotDto>();

            // Convert TimeOnly to TimeSpan for proper operations
            var currentTime = operatingHours.OpenTime.Value.ToTimeSpan();
            var endTime = operatingHours.CloseTime.Value.ToTimeSpan();
            var slotDuration = TimeSpan.FromMinutes(30); // Default slot duration

            // For today, skip past time slots
            if (date.Date == DateTime.UtcNow.Date)
            {
                var now = DateTime.UtcNow.TimeOfDay;
                if (now > currentTime)
                {
                    // Round up to next slot
                    var minutesSinceOpen = (now - currentTime).TotalMinutes;
                    var slotsToSkip = (int)Math.Ceiling(minutesSinceOpen / 30.0);
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(slotsToSkip * 30));
                }
            }

            while (currentTime.Add(slotDuration) <= endTime)
            {
                var slotEnd = currentTime.Add(slotDuration);

                // Convert TimeSpan back to TimeOnly for comparison with appointments
                var slotStartTimeOnly = TimeOnly.FromTimeSpan(currentTime);
                var slotEndTimeOnly = TimeOnly.FromTimeSpan(slotEnd);

                // Check for conflicts
                bool isConflicted = confirmedAppointments.Any(a =>
                    a.StartTime < slotEndTimeOnly && a.EndTime > slotStartTimeOnly);

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
    }
}
