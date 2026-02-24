
namespace Smart_Medc.Application.DTOs.Organization
{
    public class DoctorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
