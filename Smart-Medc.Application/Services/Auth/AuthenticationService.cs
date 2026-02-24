using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common.Constants;
using Smart_Medc.Application.Common.Results;
using Smart_Medc.Application.DTOs.Auth;
using Smart_Medc.Application.Interfaces.Auth;
using Smart_Medc.Application.Interfaces.Services;
using Smart_Medc.Application.Interfaces.Services.Auth;
using Smart_Medc.Application.Interfaces.Storage;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Entities.OrganizationModels;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;
using Smart_Medc.Domain.Interfaces.Services.Auth;

namespace Smart_Medc.Application.Services.Auth
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IGoogleAuthenticatorService _googleAuthService;
        //private readonly IFileStorageService _fileStorage;
        private readonly IAdminService _adminService;
        private readonly OrganizationDocumentService _documentService;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            IOtpService otpService,
            IEmailService emailService,
            ILogger<AuthenticationService> logger,
            IGoogleAuthenticatorService googleAuthService,
            IFileStorageService fileStorage,
            IAdminService adminService,
            OrganizationDocumentService organizationDocumentService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _otpService = otpService;
            _emailService = emailService;
            _logger = logger;
            _googleAuthService = googleAuthService;
            //_fileStorage = fileStorage;
            _documentService = organizationDocumentService;
            _adminService = adminService;
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null)
        {
            var user = await _userManager.Users
                .Include(u => u.Patient)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return Result<AuthResponse>.Failure("Invalid email or password");

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
                return Result<AuthResponse>.Failure("Invalid email or password");

            if (user.UserType == UserType.Organization && user.Organization != null)
            {
                switch (user.Organization.VerificationStatus)
                {
                    case VerificationStatus.Pending:
                        _logger.LogWarning("Login attempt by pending organization: {Email}", user.Email);
                        return Result<AuthResponse>.Failure(
                            "Your organization is pending verification. Please wait for admin approval.",
                            "ORGANIZATION_PENDING_VERIFICATION");

                    case VerificationStatus.Rejected:
                        _logger.LogWarning("Login attempt by rejected organization: {Email}", user.Email);
                        return Result<AuthResponse>.Failure(
                            "Your organization verification was rejected. Please contact support for more information.",
                            "ORGANIZATION_VERIFICATION_REJECTED");

                    case VerificationStatus.Suspended:
                        _logger.LogWarning("Login attempt by organization under review: {Email}", user.Email);
                        return Result<AuthResponse>.Failure(
                            "Your organization is currently under review. Please wait for admin approval.",
                            "ORGANIZATION_UNDER_REVIEW");

                    case VerificationStatus.Verified:
                        break;

                    default:
                        _logger.LogError("Unknown verification status for organization: {Email}", user.Email);
                        return Result<AuthResponse>.Failure("Invalid account status. Please contact support.");
                }
            }

            if (user.UserType == UserType.Patient && !user.IsEmailVerified)
            {
                return Result<AuthResponse>.Failure(
                    "Email not verified. Please check your email for verification code.",
                    "EMAIL_VERIFICATION_REQUIRED");
            }

            if (!user.IsActive)
                return Result<AuthResponse>.Failure("Account is deactivated. Please contact support.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return Result<AuthResponse>.Failure("Account locked due to multiple failed login attempts");

            if (result.IsNotAllowed)
                return Result<AuthResponse>.Failure("Login not allowed. Please verify your email or phone");

            if (!result.Succeeded)
                return Result<AuthResponse>.Failure("Invalid email or password");

            if (user.TwoFactorEnabled && user.IsTwoFactorAuthenticatorEnabled)
            {
                _logger.LogInformation("2FA required for user {Email}", user.Email);

                return Result<AuthResponse>.Failure(
                    "Two-factor authentication required. Please enter the code from your authenticator app.",
                    "2FA_REQUIRED");
            }

            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            await _tokenService.CreateRefreshTokenAsync(user, refreshToken, ipAddress);

            var response = await CreateAuthResponseAsync(user, accessToken, refreshToken);

            _logger.LogInformation("User {Email} logged in successfully from IP {IP}", user.Email, ipAddress);

            return Result<AuthResponse>.Success(response, "Login successful");
        }

        public async Task<Result<Guid>> RegisterPatientAsync(RegisterPatientRequest request, string? ipAddress = null)
        {
            if (await _userManager.FindByEmailAsync(request.Email) != null)
                return Result<Guid>.Failure("Email already exist. Please enter new email.");

            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
                UserType = UserType.Patient,
                EmailConfirmed = false,
                PhoneNumberConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var userResult = await _userManager.CreateAsync(user, request.Password);

            if (!userResult.Succeeded)
                return Result<Guid>.Failure("Registration failed", userResult.Errors.Select(e => e.Description).ToArray());

            await _userManager.AddToRoleAsync(user, AppRoles.Patient);

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                EmergencyContactRelationship = request.EmergencyContactRelationship,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Patients.AddAsync(patient);
            await _unitOfWork.SaveChangesAsync();

            var otpSent = await _otpService.SendOtpAsync(user, OtpPurpose.EmailVerification);

            if (!otpSent)
            {
                _logger.LogWarning("Failed to send OTP to {Email} after registration", user.Email);
            }

            _logger.LogInformation("Send OTP successfully and patient registered with email {Email}, UserId: {UserId}", user.Email, user.Id);

            return Result<Guid>.Success(user.Id, "Registration successful! Please check your email for verification code.");
        }

        public async Task<Result> RegisterOrganizationAsync(RegisterOrganizationRequest request, string? ipAddress = null)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(request.Email) != null)
                    return Result.Failure("Email already registered");

                var orgType = request.OrganizationType;

                var user = new ApplicationUser
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    UserName = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    UserType = UserType.Organization,
                    EmailConfirmed = true, // Organizations are email confirmed by default
                    PhoneNumberConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var userResult = await _userManager.CreateAsync(user, request.Password);

                if (!userResult.Succeeded)
                {
                    return Result.Failure(
                        "Registration failed",
                        userResult.Errors.Select(e => e.Description).ToArray());
                }

                await _userManager.AddToRoleAsync(user, AppRoles.Organization);

                var organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = request.OrganizationName,
                    Type = orgType,
                    Description = request.Description,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    ZipCode = request.ZipCode,
                    Country = request.Country,
                    Website = request.Website,
                    SSN = request.SSN,
                    VerificationStatus = VerificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow,

                    // Default settings
                    //AllowSameDayBooking = false,
                    //MinCancellationNoticeHours = 24,
                    //MaxReschedulesAllowed = 2,
                    //AutoRejectDays = 3
                };

                await _unitOfWork.Organizations.AddAsync(organization);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Organization created: {OrganizationName} (ID: {OrganizationId})",
                    organization.Name,
                    organization.Id);

                if (request.Documents != null && request.Documents.Any())
                {
                    try
                    {
                        var uploadedDocs = await _documentService.UploadDocumentsDuringRegistrationAsync(
                            organization.Id,
                            request.Documents,
                            request.DocumentTypes,
                            request.DocumentNames);

                        _logger.LogInformation(
                            "Uploaded {Count} documents for organization {OrganizationId}",
                            uploadedDocs.Count,
                            organization.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error uploading documents for organization {OrganizationId}. Registration continues.",
                            organization.Id);

                    }
                }

                try
                {
                    await _adminService.SendOrganizationRegistrationNotificationAsync(organization.Id);

                    _logger.LogInformation(
                        "Admin notification sent for organization {OrganizationId}",
                        organization.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send admin notification for organization {OrganizationId}",
                        organization.Id);


                }

                user = await _userManager.Users
                    .Include(u => u.Organization)
                        .ThenInclude(o => o.Documents)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (user == null)
                    return Result.Failure("User not found after registration");

                // Organizations must be approved by admin before they can login and not generate access token.

                _logger.LogInformation(
                    "Organization {OrganizationName} registered successfully with email {Email} from IP {IP}. Status: Pending",
                    organization.Name,
                    request.Email,
                    ipAddress);

                return Result.Success(
                    "Organization registered successfully. Your account is pending verification. You will receive an email once approved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during organization registration for email {Email}", request.Email);
                return Result.Failure("Registration failed. Please try again later.");
            }
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
        {
            var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);

            if (storedToken == null || !storedToken.IsActive)
                return Result<AuthResponse>.Failure("Invalid or expired refresh token");

            var user = await _userManager.Users
                .Include(u => u.Patient)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);

            if (user == null || !user.IsActive)
                return Result<AuthResponse>.Failure("User not found or inactive");

            if (user.UserType == UserType.Organization && user.Organization != null)
            {
                if (user.Organization.VerificationStatus != VerificationStatus.Verified)
                {
                    _logger.LogWarning(
                        "Token refresh attempt by non-verified organization: {Email}, Status: {Status}",
                        user.Email,
                        user.Organization.VerificationStatus);

                    storedToken.RevokedAt = DateTime.UtcNow;
                    storedToken.RevokedByIp = ipAddress;
                    storedToken.ReasonRevoked = "Organization verification status changed";
                    await _unitOfWork.RefreshTokens.UpdateAsync(storedToken);
                    await _unitOfWork.SaveChangesAsync();

                    return Result<AuthResponse>.Failure(
                        "Your organization verification status has changed. Please login again.",
                        "ORGANIZATION_VERIFICATION_CHANGED");
                }
            }

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;
            storedToken.ReasonRevoked = "Replaced by new token";

            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();

            storedToken.ReplacedByToken = newRefreshToken;
            await _unitOfWork.RefreshTokens.UpdateAsync(storedToken);

            await _tokenService.CreateRefreshTokenAsync(user, newRefreshToken, ipAddress);

            var response = await CreateAuthResponseAsync(user, newAccessToken, newRefreshToken);

            return Result<AuthResponse>.Success(response, "Token refreshed successfully");
        }

        public async Task<Result> LogoutAsync(string refreshToken, string? ipAddress = null)
        {
            await _tokenService.RevokeRefreshTokenAsync(refreshToken, ipAddress, "Logged out by user");
            return Result.Success("Logout successful");
        }

        public async Task<Result> RevokeAllTokensAsync(Guid userId)
        {
            await _tokenService.RevokeAllUserRefreshTokensAsync(userId, "All tokens revoked by user");
            return Result.Success("All tokens revoked successfully");
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
                return Result.Failure("Password change failed", result.Errors.Select(e => e.Description).ToArray());

            await _tokenService.RevokeAllUserRefreshTokensAsync(userId, "Password changed");

            return Result.Success("Password changed successfully. Please login again.");
        }

        public async Task<Result<AuthResponse>> VerifyTwoFactorAsync(TwoFactorVerificationRequest request, string? ipAddress = null)
        {
            // TODO: Implement 2FA email verification logic
            throw new NotImplementedException("Two-factor authentication not yet implemented");
        }

        private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, string accessToken, string refreshToken)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                ProfileImageUrl = user.ProfileImageUrl,
                UserType = user.UserType.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                IsPhoneVerified = user.IsPhoneVerified,
                Roles = roles.ToList()
            };

            if (user.Patient != null)
            {
                userDto.PatientId = user.Patient.Id;
            }

            if (user.Organization != null)
            {
                userDto.OrganizationId = user.Organization.Id;
                userDto.OrganizationName = user.Organization.Name;
                userDto.VerificationStatus = user.Organization.VerificationStatus.ToString();
            }

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                User = userDto
            };
        }

        public async Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, string? ipAddress = null)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result<AuthResponse>.Failure("User not found");

            if (user.IsEmailVerified)
                return Result<AuthResponse>.Failure("Email already verified");

            var isValidOtp = await _otpService.ValidateOtpAsync(
                user.Id,
                request.OtpCode,
                OtpPurpose.EmailVerification);

            if (!isValidOtp)
                return Result<AuthResponse>.Failure("Invalid or expired OTP code");

            user.IsEmailVerified = true;
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Result<AuthResponse>.Failure("Failed to verify email", result.Errors.Select(e => e.Description).ToArray());

            await _emailService.SendWelcomeEmailAsync(user.Email ?? string.Empty, user.FirstName);

            _logger.LogInformation("Email verified successfully for user {Email}", user.Email);

            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            await _tokenService.CreateRefreshTokenAsync(user, refreshToken, ipAddress);

            var response = await CreateAuthResponseAsync(user, accessToken, refreshToken);

            _logger.LogInformation("Email verified successfully for user {Email}", user.Email);

            return Result<AuthResponse>.Success(response, "Email verified successfully");
        }

        public async Task<Result> ResendEmailVerificationOtpAsync(ResendOtpRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure("User not found");

            if (user.IsEmailVerified)
                return Result.Failure("Email already verified");

            if (user.UserType != UserType.Patient)
                return Result.Failure("Email verification is only available for patients");

            await _otpService.InvalidateOtpAsync(user.Id, OtpPurpose.EmailVerification);

            var otpSent = await _otpService.SendOtpAsync(user, OtpPurpose.EmailVerification);

            if (!otpSent)
                return Result.Failure("Failed to send verification email. Please try again later.");

            _logger.LogInformation("OTP resent to {Email}", user.Email);

            return Result.Success("Verification code sent to your email");
        }

        public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                return Result.Success("If the email exists, a verification code has been sent.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Password reset requested for inactive account: {Email}", request.Email);
                return Result.Success("If the email exists, a verification code has been sent.");
            }

            await _otpService.InvalidateOtpAsync(user.Id, OtpPurpose.PasswordReset);

            var otpSent = await _otpService.SendOtpAsync(user, OtpPurpose.PasswordReset);

            if (!otpSent)
            {
                _logger.LogError("Failed to send password reset OTP to {Email}", user.Email);
                return Result.Failure("Failed to send verification code. Please try again later.");
            }

            _logger.LogInformation("Password reset OTP sent to {Email}", user.Email);

            return Result.Success("Verification code sent to your email");
        }

        public async Task<Result> VerifyPasswordResetCodeAsync(VerifyPasswordResetCodeRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Result.Failure("Invalid verification code");
            }

            var otpVerifications = await _unitOfWork.OtpVerifications.FindAsync(
                otp => otp.UserId == user.Id &&
                       otp.Purpose == OtpPurpose.PasswordReset &&
                       !otp.IsUsed &&
                       otp.ExpiresAt > DateTime.UtcNow);

            var otpVerification = otpVerifications
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (otpVerification == null)
            {
                _logger.LogWarning("No valid OTP found for password reset for {Email}", user.Email);
                return Result.Failure("Invalid or expired verification code");
            }

            if (otpVerification.AttemptCount >= 5)
            {
                _logger.LogWarning("Max OTP attempts exceeded for password reset for {Email}", user.Email);
                return Result.Failure("Maximum verification attempts exceeded. Please request a new code.");
            }

            if (otpVerification.Code != request.OtpCode)
            {
                otpVerification.AttemptCount++;
                await _unitOfWork.OtpVerifications.UpdateAsync(otpVerification);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning("Invalid password reset OTP attempt for {Email}", user.Email);
                return Result.Failure("Invalid or expired verification code");
            }

            otpVerification.AttemptCount++;
            await _unitOfWork.OtpVerifications.UpdateAsync(otpVerification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password reset code verified for {Email}", user.Email);
            return Result.Success("Verification code validated successfully");
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Result.Failure("Invalid request");
            }

            var isValidOtp = await _otpService.ValidateOtpAsync(
                user.Id,
                request.OtpCode,
                OtpPurpose.PasswordReset);

            if (!isValidOtp)
            {
                _logger.LogWarning("Invalid OTP during password reset for {Email}", user.Email);
                return Result.Failure("Invalid or expired verification code");
            }

            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                _logger.LogError("Failed to remove old password for {Email}", user.Email);
                return Result.Failure("Failed to reset password");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                _logger.LogError("Failed to add new password for {Email}", user.Email);
                return Result.Failure("Failed to reset password",
                    addPasswordResult.Errors.Select(e => e.Description).ToArray());
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _tokenService.RevokeAllUserRefreshTokensAsync(user.Id, "Password reset");

            _logger.LogInformation("Password reset successful for {Email}", user.Email);

            return Result.Success("Password reset successfully. Please login with your new password.");
        }

        #region Google Authenticator 2FA

        public async Task<Result<Enable2FAResponse>> Enable2FAAuthenticatorAsync(Guid userId, Enable2FARequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result<Enable2FAResponse>.Failure("User not found");

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
                return Result<Enable2FAResponse>.Failure("Invalid password. Please try again.");

            if (user.TwoFactorEnabled && user.IsTwoFactorAuthenticatorEnabled)
                return Result<Enable2FAResponse>.Failure("Two-factor authentication is already enabled");

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user)
                    ?? throw new InvalidOperationException("Failed to generate authenticator key");
            }

            var qrCodeUrl = _googleAuthService.GenerateQrCodeUrl(user.Email ?? string.Empty, unformattedKey);
            var manualEntryKey = _googleAuthService.FormatSecretKeyForDisplay(unformattedKey);

            var response = new Enable2FAResponse
            {
                SecretKey = unformattedKey,
                QrCodeUrl = qrCodeUrl,
                ManualEntryKey = manualEntryKey,
                Email = user.Email ?? string.Empty,
                Issuer = "Smart Medi"
            };

            _logger.LogInformation("2FA setup initiated for user {Email}", user.Email);

            return Result<Enable2FAResponse>.Success(response,
                "Scan the QR code with Google Authenticator app and enter the 6-digit code to complete setup");
        }

        public async Task<Result> Verify2FASetupAsync(Guid userId, Verify2FASetupRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
                return Result.Failure("2FA setup not initiated. Please start the setup process first.");

            var isValid = _googleAuthService.ValidateCode(unformattedKey, request.Code);

            if (!isValid)
            {
                _logger.LogWarning("Invalid 2FA setup code for user {Email}", user.Email);
                return Result.Failure("Invalid verification code. Please check the code in your authenticator app and try again.");
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!result.Succeeded)
                return Result.Failure("Failed to enable 2FA", result.Errors.Select(e => e.Description).ToArray());

            user.IsTwoFactorAuthenticatorEnabled = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("2FA enabled successfully for user {Email}", user.Email);

            return Result.Success("Two-factor authentication enabled successfully! You will now need to enter a code from Google Authenticator when logging in.");
        }

        public async Task<Result> Disable2FAAuthenticatorAsync(Guid userId, Disable2FARequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found");

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
                return Result.Failure("Invalid password. Please try again.");

            if (!user.TwoFactorEnabled || !user.IsTwoFactorAuthenticatorEnabled)
                return Result.Failure("Two-factor authentication is not enabled");

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
                return Result.Failure("2FA configuration is invalid. Please contact support.");

            var isValidCode = _googleAuthService.ValidateCode(unformattedKey, request.Code);
            if (!isValidCode)
            {
                _logger.LogWarning("Invalid 2FA code during disable attempt for user {Email}", user.Email);
                return Result.Failure("Invalid verification code from authenticator app");
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!result.Succeeded)
                return Result.Failure("Failed to disable 2FA", result.Errors.Select(e => e.Description).ToArray());

            await _userManager.ResetAuthenticatorKeyAsync(user);

            user.IsTwoFactorAuthenticatorEnabled = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var backupCodes = await _unitOfWork.BackupCodes.FindAsync(bc => bc.UserId == userId);
            foreach (var code in backupCodes)
            {
                await _unitOfWork.BackupCodes.DeleteAsync(code);
            }
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("2FA disabled for user {Email}", user.Email);

            return Result.Success("Two-factor authentication disabled successfully");
        }

        public async Task<Result<TwoFactorStatusResponse>> Get2FAStatusAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result<TwoFactorStatusResponse>.Failure("User not found");

            var backupCodes = await _unitOfWork.BackupCodes.FindAsync(
                bc => bc.UserId == userId && !bc.IsUsed);

            var hasKey = !string.IsNullOrEmpty(await _userManager.GetAuthenticatorKeyAsync(user));

            var response = new TwoFactorStatusResponse
            {
                IsEnabled = user.TwoFactorEnabled,
                IsAuthenticatorEnabled = user.IsTwoFactorAuthenticatorEnabled,
                Email = user.Email ?? string.Empty,
                EnabledAt = user.IsTwoFactorAuthenticatorEnabled ? user.UpdatedAt : null,
                BackupCodesRemaining = backupCodes.Count(),
                HasAuthenticatorKey = hasKey
            };

            return Result<TwoFactorStatusResponse>.Success(response, "2FA status retrieved successfully");
        }

        public async Task<Result<AuthResponse>> Verify2FALoginAsync(Verify2FALoginRequest request, string? ipAddress = null)
        {
            var user = await _userManager.Users
                .Include(u => u.Patient)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("2FA login attempt for non-existent user: {Email}", request.Email);
                return Result<AuthResponse>.Failure("Invalid verification code");
            }

            if (user.UserType == UserType.Organization && user.Organization != null)
            {
                switch (user.Organization.VerificationStatus)
                {
                    case VerificationStatus.Pending:
                        _logger.LogWarning("2FA login attempt by pending organization: {Email}", user.Email);
                        return Result<AuthResponse>.Failure(
                            "Your organization is pending verification. Please wait for admin approval.",
                            "ORGANIZATION_PENDING_VERIFICATION");

                    case VerificationStatus.Rejected:
                        _logger.LogWarning("2FA login attempt by rejected organization: {Email}", user.Email);
                        return Result<AuthResponse>.Failure(
                            "Your organization verification was rejected. Please contact support.",
                            "ORGANIZATION_VERIFICATION_REJECTED");

                    case VerificationStatus.Suspended:
                        _logger.LogWarning("2FA login attempt by organization under review: {Email}", user.Email);
                        return Result<AuthResponse>.Failure(
                            "Your organization is under review. Please wait for admin approval.",
                            "ORGANIZATION_UNDER_REVIEW");

                    case VerificationStatus.Verified:
                        break;

                    default:
                        _logger.LogError("Unknown verification status for organization: {Email}", user.Email);
                        return Result<AuthResponse>.Failure("Invalid account status. Please contact support.");
                }
            }

            if (!user.TwoFactorEnabled || !user.IsTwoFactorAuthenticatorEnabled)
                return Result<AuthResponse>.Failure("Two-factor authentication is not enabled for this account");

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
                return Result<AuthResponse>.Failure("2FA configuration is invalid. Please contact support.");

            var isValidCode = _googleAuthService.ValidateCode(unformattedKey, request.Code);

            if (!isValidCode)
            {
                var backupCodesList = await _unitOfWork.BackupCodes.FindAsync(
                    bc => bc.UserId == user.Id &&
                          bc.Code == request.Code &&
                          !bc.IsUsed);

                var backup = backupCodesList.FirstOrDefault();
                if (backup != null)
                {
                    backup.IsUsed = true;
                    backup.UsedAt = DateTime.UtcNow;
                    await _unitOfWork.BackupCodes.UpdateAsync(backup);
                    await _unitOfWork.SaveChangesAsync();

                    isValidCode = true;
                    _logger.LogInformation("Backup code used for user {Email}. Remaining codes: {Count}",
                        user.Email, await _unitOfWork.BackupCodes.CountAsync(bc => bc.UserId == user.Id && !bc.IsUsed));
                }
            }

            if (!isValidCode)
            {
                _logger.LogWarning("Invalid 2FA code attempt for user {Email} from IP {IP}", user.Email, ipAddress);
                return Result<AuthResponse>.Failure("Invalid verification code. Please check your authenticator app or use a backup code.");
            }

            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            await _tokenService.CreateRefreshTokenAsync(user, refreshToken, ipAddress);

            var response = await CreateAuthResponseAsync(user, accessToken, refreshToken);

            _logger.LogInformation("2FA verification successful for user {Email} from IP {IP}", user.Email, ipAddress);

            return Result<AuthResponse>.Success(response, "Login successful");
        }

        public async Task<Result<List<string>>> GenerateBackupCodesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result<List<string>>.Failure("User not found");

            if (!user.TwoFactorEnabled || !user.IsTwoFactorAuthenticatorEnabled)
                return Result<List<string>>.Failure("Two-factor authentication must be enabled first");

            var oldCodes = await _unitOfWork.BackupCodes.FindAsync(bc => bc.UserId == userId);
            foreach (var oldCode in oldCodes)
            {
                await _unitOfWork.BackupCodes.DeleteAsync(oldCode);
            }
            await _unitOfWork.SaveChangesAsync();

            var backupCodes = new List<string>();
            var random = new Random();

            for (int i = 0; i < 10; i++)
            {
                var code = GenerateBackupCode(random);
                backupCodes.Add(code);

                var backupCode = new Domain.Entities.Identity.BackupCode
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Code = code,
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _unitOfWork.BackupCodes.AddAsync(backupCode);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Generated {Count} new backup codes for user {UserId}", backupCodes.Count, userId);

            return Result<List<string>>.Success(backupCodes,
                "Backup codes generated successfully. Save these codes in a safe place - each can only be used once.");
        }

        private static string GenerateBackupCode(Random random)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new char[8];
            for (int i = 0; i < 8; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }
            return new string(code);
        }

        #endregion
    }
}