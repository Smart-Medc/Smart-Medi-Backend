using Smart_Medc.Application.Common.Results;
using Smart_Medc.Application.DTOs.Auth;

namespace Smart_Medc.Application.Interfaces.Services.Auth
{
    public interface IAuthenticationService
    {
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null);
        Task<Result<Guid>> RegisterPatientAsync(RegisterPatientRequest request, string? ipAddress = null);
        Task<Result> RegisterOrganizationAsync(RegisterOrganizationRequest request, string? ipAddress = null);
        Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
        Task<Result> LogoutAsync(string refreshToken, string? ipAddress = null);
        Task<Result> RevokeAllTokensAsync(Guid userId);
        Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<Result<AuthResponse>> VerifyTwoFactorAsync(TwoFactorVerificationRequest request, string? ipAddress = null);
        Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, string? ipAddress = null);
        Task<Result> ResendEmailVerificationOtpAsync(ResendOtpRequest request);
        Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<Result> VerifyPasswordResetCodeAsync(VerifyPasswordResetCodeRequest request);
        Task<Result> ResetPasswordAsync(ResetPasswordRequest request);



        Task<Result<Enable2FAResponse>> Enable2FAAuthenticatorAsync(Guid userId, Enable2FARequest request);

        Task<Result> Verify2FASetupAsync(Guid userId, Verify2FASetupRequest request);
        Task<Result> Disable2FAAuthenticatorAsync(Guid userId, Disable2FARequest request);
        Task<Result<TwoFactorStatusResponse>> Get2FAStatusAsync(Guid userId);
        Task<Result<AuthResponse>> Verify2FALoginAsync(Verify2FALoginRequest request, string? ipAddress = null);
        Task<Result<List<string>>> GenerateBackupCodesAsync(Guid userId);
    }
}
