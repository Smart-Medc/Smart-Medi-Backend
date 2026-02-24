using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Appointment
{
    public class CompleteAppointmentDto 
    {
        [MaxLength(2000)]
        public string? Notes { get; set; } 
    }
}
