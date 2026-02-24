using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class RegisterOrganizationRequest
    {
        // Personal Information
        //[Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        //[Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Organization Information
        [Required(ErrorMessage = "Organization name is required")]
        [StringLength(200, MinimumLength = 3)]
        public string OrganizationName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Organization type is required")]
        public OrganizationType OrganizationType { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        // Address Information
        [Required(ErrorMessage = "Address is required")]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        //[Url(ErrorMessage = "Invalid website URL")] // give me error 
        public string? Website { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SSN { get; set; }


        public List<IFormFile>? Documents { get; set; }
        public List<OrganizationDocumentType>? DocumentTypes { get; set; }
        public List<string>? DocumentNames { get; set; }
    }
}