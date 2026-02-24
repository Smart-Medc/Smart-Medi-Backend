namespace Smart_Medc.Application.DTOs.Auth
{
    public class Enable2FAResponse
    {
        /// <summary>
        /// Secret key for Google Authenticator (base32 encoded)
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// QR code as Base64 PNG image (data:image/png;base64,...)
        /// Can be used directly in <img src="..." /> tag
        /// </summary>
        public string QrCodeUrl { get; set; } = string.Empty;

        /// <summary>
        /// Manual entry key for users who can't scan QR code
        /// Formatted as: ABCD EFGH IJKL MNOP
        /// </summary>
        public string ManualEntryKey { get; set; } = string.Empty;

        /// <summary>
        /// User's email for display in authenticator app
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// App name shown in authenticator
        /// </summary>
        public string Issuer { get; set; } = "Smart Medi";
    }
}