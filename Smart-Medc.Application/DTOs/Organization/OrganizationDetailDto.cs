
namespace Smart_Medc.Application.DTOs.Organization
{
    // Details
    public class OrganizationDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public List<DoctorDto> Doctors { get; set; } = new();
        public List<OperatingHoursDto> OperatingHours { get; set; } = new();
        public List<ConsultationFeeDto> ConsultationFees { get; set; } = new();
    }
}
