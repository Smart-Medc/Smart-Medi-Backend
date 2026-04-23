
namespace Smart_Medc.Application.DTOs.DataSharing
{
    public class AccessHistoryItemDto
    {
        // The share code string (e.g. "MED-X7K9P")
        public string Code { get; set; } = string.Empty;

        // Patient name from the share code's Patient → User navigation
        public string PatientName { get; set; } = string.Empty;

        // CHANGED: same reason — DateTimeOffset for ExpiresAt
        // When this organization last accessed this code
        public DateTimeOffset AccessedAt { get; set; }

        // Expiry info so the frontend can show "Expires in X" or "Permanent"
        public DateTimeOffset? ExpiresAt { get; set; }

        // Current status of the share code (active / expired / revoked)
        public string Status { get; set; } = string.Empty;
    }
}
