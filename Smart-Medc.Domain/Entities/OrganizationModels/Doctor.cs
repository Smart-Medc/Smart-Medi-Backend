namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class Doctor
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Title { get; set; } // Dr., etc.
        public string? Specialization { get; set; }
        public string? Biography { get; set; }
        public string? PhotoUrl { get; set; }

        public decimal AverageRating { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }
}
