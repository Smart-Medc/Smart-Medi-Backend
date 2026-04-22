using System.ComponentModel.DataAnnotations;

namespace Smart_Medc.Application.DTOs.Appointment
{
    public class ConfirmAppointmentDto
    {
        [MaxLength(2000)]
        public string? PreparationInstructions { get; set; }
    }
}
