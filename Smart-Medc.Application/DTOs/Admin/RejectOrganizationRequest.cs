using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Admin
{
    public class RejectOrganizationRequest
    {
        [Required(ErrorMessage = "Organization ID is required")]
        public Guid OrganizationId { get; set; }

        [Required(ErrorMessage = "Rejection reason is required")]
        [StringLength(1000, MinimumLength = 5)]
        public string RejectionReason { get; set; } = string.Empty;
    }
}