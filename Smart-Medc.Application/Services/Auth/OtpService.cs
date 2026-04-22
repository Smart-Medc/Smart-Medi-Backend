using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Interfaces.Auth;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;
using System.Security.Cryptography;

namespace Smart_Medc.Application.Services.Auth
{
    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<OtpService> _logger;
        private const int OTP_LENGTH = 6;
        private const int OTP_EXPIRATION_MINUTES = 5;
        private const int MAX_OTP_ATTEMPTS = 5;

        public OtpService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<OtpService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<string> GenerateOtpAsync(Guid userId, OtpPurpose purpose, OtpDeliveryMethod deliveryMethod)
        {
            var otpCode = GenerateSecureOtp();

            var otpVerification = new OtpVerification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Code = otpCode,
                Purpose = purpose,
                DeliveryMethod = deliveryMethod,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRATION_MINUTES),
                CreatedAt = DateTime.UtcNow,
                IsUsed = false,
                AttemptCount = 0
            };

            await _unitOfWork.OtpVerifications.AddAsync(otpVerification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("OTP generated for user {UserId} with purpose {Purpose}", userId, purpose);

            return otpCode;
        }

        public async Task<bool> ValidateOtpAsync(Guid userId, string code, OtpPurpose purpose)
        {
            var otpVerifications = await _unitOfWork.OtpVerifications.FindAsync(
                otp => otp.UserId == userId &&
                       otp.Purpose == purpose &&
                       !otp.IsUsed &&
                       otp.ExpiresAt > DateTime.UtcNow);

            var otpVerification = otpVerifications
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (otpVerification == null)
            {
                _logger.LogWarning("No valid OTP found for user {UserId}", userId);
                return false;
            }

            otpVerification.AttemptCount++;
            await _unitOfWork.OtpVerifications.UpdateAsync(otpVerification);
            await _unitOfWork.SaveChangesAsync();

            if (otpVerification.AttemptCount > MAX_OTP_ATTEMPTS)
            {
                _logger.LogWarning("Max OTP attempts exceeded for user {UserId}", userId);
                return false;
            }

            if (otpVerification.Code != code)
            {
                _logger.LogWarning("Invalid OTP code for user {UserId}", userId);
                return false;
            }

            otpVerification.IsUsed = true;
            otpVerification.UsedAt = DateTime.UtcNow;
            await _unitOfWork.OtpVerifications.UpdateAsync(otpVerification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("OTP validated successfully for user {UserId}", userId);
            return true;
        }

        public async Task<bool> SendOtpAsync(ApplicationUser user, OtpPurpose purpose)
        {
            try
            {
                var otpCode = await GenerateOtpAsync(user.Id, purpose, OtpDeliveryMethod.Email);

                bool emailSent = false;

                switch (purpose)
                {
                    case OtpPurpose.EmailVerification:
                        emailSent = await _emailService.SendOtpEmailAsync(
                            user.Email ?? string.Empty,
                            otpCode,
                            user.FirstName);
                        break;

                    case OtpPurpose.PasswordReset:
                        emailSent = await _emailService.SendPasswordResetOtpEmailAsync(
                            user.Email ?? string.Empty,
                            otpCode,
                            user.FirstName);
                        break;

                    default:
                        _logger.LogWarning("Unsupported OTP purpose: {Purpose}", purpose);
                        return false;
                }

                if (emailSent)
                {
                    _logger.LogInformation("OTP sent successfully to {Email} for {Purpose}", user.Email, purpose);
                    return true;
                }

                _logger.LogError("Failed to send OTP email to {Email}", user.Email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to user {UserId}", user.Id);
                return false;
            }
        }

        public async Task InvalidateOtpAsync(Guid userId, OtpPurpose purpose)
        {
            var otpVerifications = await _unitOfWork.OtpVerifications.FindAsync(
                otp => otp.UserId == userId &&
                       otp.Purpose == purpose &&
                       !otp.IsUsed);

            foreach (var otp in otpVerifications)
            {
                otp.IsUsed = true;
                await _unitOfWork.OtpVerifications.UpdateAsync(otp);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("All OTP codes invalidated for user {UserId} with purpose {Purpose}", userId, purpose);
        }

        private static string GenerateSecureOtp()
        {
            var otp = new char[OTP_LENGTH];
            var randomNumber = RandomNumberGenerator.GetBytes(OTP_LENGTH);

            for (int i = 0; i < OTP_LENGTH; i++)
            {
                otp[i] = (char)('0' + (randomNumber[i] % 10));
            }

            return new string(otp);
        }
    }
}