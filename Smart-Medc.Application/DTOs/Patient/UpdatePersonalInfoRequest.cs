using System.ComponentModel.DataAnnotations;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Patient
{
    public class UpdatePersonalInfoRequest
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public Gender Gender { get; set; }

        public BloodType? BloodType { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string? PhoneNumber { get; set; }

        [StringLength(1000, ErrorMessage = "Allergies information cannot exceed 1000 characters")]
        public string? Allergies { get; set; }

        [Required(ErrorMessage = "Please indicate if there are known allergies")]
        public bool HasNoKnownAllergies { get; set; }

        [Required(ErrorMessage = "Emergency contact name is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Emergency contact name must be between 1 and 200 characters")]
        public string ContactName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Emergency contact phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Relationship is required")]
        public EmergencyContactRelationship Relationship { get; set; }
    }
}