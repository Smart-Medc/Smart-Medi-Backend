using Smart_Medc.Application.DTOs.Auth;

namespace Smart_Medc.Application.DTOs.Admin
{
    public class OrganizationDetailsDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Contact Information
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;

        // Address
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        // Verification
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
        public string? VerifiedByEmail { get; set; }
        public string? RejectionReason { get; set; }

        // Documents
        public List<DocumentDto> Documents { get; set; } = new();

        // Additional Info
        public string? Website { get; set; }
        public string? SSN { get; set; }
        public bool IsActive { get; set; }
    }
}