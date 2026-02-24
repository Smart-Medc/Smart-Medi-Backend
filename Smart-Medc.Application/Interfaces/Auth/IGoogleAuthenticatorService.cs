namespace Smart_Medc.Application.Interfaces.Auth
{
    public interface IGoogleAuthenticatorService
    {
        string GenerateQrCodeUrl(string email, string secretKey, string issuer = "Smart Medi");
        bool ValidateCode(string secretKey, string code);
        string FormatSecretKeyForDisplay(string secretKey);
    }
}