namespace Smart_Medc.Application.DTOs.Admin
{
    public class PatientListItemDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Emergency Contact
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyContactRelationship { get; set; } = string.Empty;
    }
}