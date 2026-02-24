
using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Appointment
{
    public class RescheduleAppointmentDto
    {
        [Required]
        public DateTime NewDate { get; set; }

        [Required]
        public TimeOnly NewStartTime { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
