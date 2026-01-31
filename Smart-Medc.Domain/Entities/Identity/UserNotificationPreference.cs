using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Enums;

namespace Domain.Entities.Identity;

public class UserNotificationPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ApplicationUser User { get; set; } = null!;
}

