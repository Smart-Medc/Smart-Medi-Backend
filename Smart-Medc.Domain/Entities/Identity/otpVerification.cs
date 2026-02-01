using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.Identity;

public class OtpVerification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public OtpDeliveryMethod DeliveryMethod { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public int AttemptCount { get; set; } = 0;

    // Navigation
    public virtual ApplicationUser User { get; set; } = null!;
}