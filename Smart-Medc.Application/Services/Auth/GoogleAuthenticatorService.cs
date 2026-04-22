using OtpNet;
using QRCoder;
using Smart_Medc.Application.Interfaces.Auth;
using System.Text;

namespace Smart_Medc.Application.Services.Auth
{
    public class GoogleAuthenticatorService : IGoogleAuthenticatorService
    {
        private const string DefaultIssuer = "Smart Medi";

        /// <summary>
        /// Generate QR code as Base64 PNG image
        /// </summary>
        public string GenerateQrCodeUrl(string email, string secretKey, string issuer = DefaultIssuer)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentException("Secret key cannot be empty", nameof(secretKey));

            var otpAuthUrl = GenerateOtpAuthUrl(email, secretKey, issuer);

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeBytes = qrCode.GetGraphic(20); // 20 pixels per module
            var base64String = Convert.ToBase64String(qrCodeBytes);

            return $"data:image/png;base64,{base64String}";
        }

        /// <summary>
        /// Generate OTP Auth URL for authenticator apps
        /// </summary>
        private string GenerateOtpAuthUrl(string email, string secretKey, string issuer)
        {
            // otpauth://totp/Issuer:email?secret=SECRET&issuer=Issuer&algorithm=SHA1&digits=6&period=30
            var encodedEmail = Uri.EscapeDataString(email);
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedSecret = Uri.EscapeDataString(secretKey);

            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={encodedSecret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
        }

        public bool ValidateCode(string secretKey, string code)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
                return false;

            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
                return false;

            try
            {
                var secretKeyBytes = Base32Encoding.ToBytes(secretKey);

                var totp = new Totp(secretKeyBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);

                var isValid = totp.VerifyTotp(code, out long timeStepMatched, window: new VerificationWindow(previous: 1, future: 1));

                return isValid;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string FormatSecretKeyForDisplay(string secretKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
                return string.Empty;

            var formatted = new StringBuilder();
            for (int i = 0; i < secretKey.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    formatted.Append(' ');

                formatted.Append(secretKey[i]);
            }

            return formatted.ToString();
        }
    }
}