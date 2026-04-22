namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class ConsultationFee
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public string FeeType { get; set; } = string.Empty; // e.g., "Standard Consultation", "Follow-up"
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public int DurationMinutes { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }
}
