namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class OrganizationAvailabilityException
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public DateOnly Date { get; set; }
        public bool IsFullDayOff { get; set; } = true;

        // Is triggered if IsFullDayOff = false
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }
}
