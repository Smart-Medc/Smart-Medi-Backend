namespace Smart_Medc.Application.DTOs.Admin
{
    public class OrganizationListItemDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        // Address
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Verification
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; } // Ignore it because required updated domain entities.
        public string? RejectionReason { get; set; }

        // Documents
        public int DocumentCount { get; set; }
        public string? Website { get; set; }
        public string? SSN { get; set; }
    }
}