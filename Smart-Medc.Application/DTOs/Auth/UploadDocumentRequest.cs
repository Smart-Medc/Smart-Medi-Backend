using Microsoft.AspNetCore.Http;
using Smart_Medc.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Organization
{
    public class UploadDocumentRequest
    {
        [Required(ErrorMessage = "Organization ID is required")]
        public Guid OrganizationId { get; set; }

        [Required(ErrorMessage = "Document type is required")]
        public OrganizationDocumentType DocumentType { get; set; }

        [Required(ErrorMessage = "Document name is required")]
        [StringLength(200, MinimumLength = 3)]
        public string DocumentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; } = null!;
    }
}