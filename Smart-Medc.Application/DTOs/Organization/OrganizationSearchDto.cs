
namespace Smart_Medc.Application.DTOs.Organization
{
    // Search
    public class OrganizationSearchDto
    {
        public string? SearchTerm { get; set; }
        public string? City { get; set; }
        public int? Type { get; set; } // Using int for filter matching with enum in Repo
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
