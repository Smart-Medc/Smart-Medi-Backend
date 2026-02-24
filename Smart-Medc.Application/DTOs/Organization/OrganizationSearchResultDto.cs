
namespace Smart_Medc.Application.DTOs.Organization
{
    public class OrganizationSearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? NextAvailable { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public List<string> Specializations { get; set; } = new();
    }
}
