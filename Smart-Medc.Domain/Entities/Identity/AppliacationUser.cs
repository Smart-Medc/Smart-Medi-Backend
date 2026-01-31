using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? ProfileImageUrl { get; set; }
    public UserType UserType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Two-Factor Authentication
    public bool IsTwoFactorEmailEnabled { get; set; } = false;
    public bool IsTwoFactorSmsEnabled { get; set; } = false;
    public bool IsTwoFactorAuthenticatorEnabled { get; set; } = false;

    // Navigation Properties
    public virtual Patient? Patient { get; set; }
    public virtual Organization? Organization { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserNotificationPreference> NotificationPreferences { get; set; } = new List<UserNotificationPreference>();
    public virtual ICollection<BackupCode> BackupCodes { get; set; } = new List<BackupCode>();
}
