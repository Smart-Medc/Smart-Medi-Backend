using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Admin
{
    public class ApproveOrganizationRequest
    {
        [Required(ErrorMessage = "Organization ID is required")]
        public Guid OrganizationId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}