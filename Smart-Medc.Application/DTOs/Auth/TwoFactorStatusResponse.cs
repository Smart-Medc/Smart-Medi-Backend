namespace Smart_Medc.Application.DTOs.Auth
{
    public class TwoFactorStatusResponse
    {
        public bool IsEnabled { get; set; }
        public bool IsAuthenticatorEnabled { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime? EnabledAt { get; set; }
        public int BackupCodesRemaining { get; set; }
        public bool HasAuthenticatorKey { get; set; }
    }
}