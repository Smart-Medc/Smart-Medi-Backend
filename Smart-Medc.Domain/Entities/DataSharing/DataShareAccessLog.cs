
using Smart_Medc.Domain.Entities.OrganizationModels;

namespace Smart_Medc.Domain.Entities.DataSharing
{
    public class DataShareAccessLog
    {
        public Guid Id { get; set; }
        public Guid DataShareCodeId { get; set; }
        public Guid? OrganizationId { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Location { get; set; }

        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual DataShareCode DataShareCode { get; set; } = null!;
        public virtual Organization? Organization { get; set; }
    }
}
