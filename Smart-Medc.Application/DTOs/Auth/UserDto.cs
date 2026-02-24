namespace Smart_Medc.Application.DTOs.Auth
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string UserType { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public List<string> Roles { get; set; } = new();

        // Patient specific
        public Guid? PatientId { get; set; }

        // Organization specific
        public Guid? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? VerificationStatus { get; set; }
    }
}