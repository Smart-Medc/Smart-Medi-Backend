using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Appointment
{
    public class RejectAppointmentDto 
    {
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty; 
    }
}
