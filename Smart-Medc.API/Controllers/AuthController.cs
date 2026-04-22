/// <summary>
/// DONE WITH TESTS:  Patient verification, refreshtokens, changepassword, resetpassword and Otp logic + Emails.
/// DONE WITH TESTS: 2FA logic (Google Authenticator) and Backup codes for 2FA recovery
/// DONE WITH TESTS: Organization verification
/// DONE WITH TESTS: Admin Panel for managing users and organizations
///  <summary>


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Medc.Application.DTOs.Auth;
using Smart_Medc.Application.Interfaces.Services.Auth;
using System.Security.Claims;

namespace Smart_Medc.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login with email and password and 2FA logic if enabled
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(request, ipAddress);

            if (!result.IsSuccess)
            {
                if (result.Errors.Contains("EMAIL_VERIFICATION_REQUIRED"))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors,
                        requiresEmailVerification = true
                    });
                }

                if (result.Errors.Contains("2FA_REQUIRED"))
                {
                    return Ok(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors,
                        requires2FA = true,
                        email = request.Email
                    });
                }

                return Unauthorized(new { success = false, message = result.Message, errors = result.Errors });
            }

            // Store refresh token in HttpOnly cookie
            SetRefreshTokenCookie(result.Data!.RefreshToken);

            _logger.LogInformation("User {Email} logged in successfully from IP {IP}", request.Email, ipAddress);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = new
                {
                    accessToken = result?.Data?.AccessToken,
                    accessTokenExpiresAt = result?.Data?.AccessTokenExpiresAt,
                    user = result?.Data?.User
                }
            });
        }

        /// <summary>
        /// Register a new patient account
        /// </summary>
        [HttpPost("register/patient")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RegisterPatientAsync(request, ipAddress);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message, errors = result.Errors });

            _logger.LogInformation("New patient registered with email {Email} from IP {IP}", request.Email, ipAddress);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = new
                {
                    userId = result.Data,
                    requiresEmailVerification = true
                }
            });
        }

        /// <summary>
        /// Verify email address with OTP code (Patient only)
        /// When patient press on verify email button route him to this endpoint
        /// </summary>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var result = await _authService.VerifyEmailAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message, errors = result.Errors });

            _logger.LogInformation("Email verified successfully for {Email}", request.Email);

            // Store refresh token in HTTP-only cookie
            SetRefreshTokenCookie(result.Data!.RefreshToken);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = new
                {
                    accessToken = result?.Data?.AccessToken,
                    accessTokenExpiresAt = result?.Data?.AccessTokenExpiresAt,
                    user = result?.Data?.User
                }
            });
        }

        /// <summary>
        /// Resend email verification OTP code (Patient only)
        /// </summary>
        [HttpPost("resend-verification-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationOtp([FromBody] ResendOtpRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var result = await _authService.ResendEmailVerificationOtpAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message, errors = result.Errors });

            _logger.LogInformation("Verification OTP resent to {Email}", request.Email);

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        /// <summary>
        /// Register a new organization account with document upload
        /// </summary>
        /// <remarks>
        /// Register an organization with optional document uploads.
        /// 
        /// **Allowed Document Types:**
        /// - MedicalLicense
        /// - Certification
        /// - Insurance
        /// - TaxDocument
        /// - Other
        /// 
        /// Note: If document upload fails, registration still succeeds and documents can be uploaded later.
        /// </remarks>
        [HttpPost("register/organization")]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterOrganization([FromForm] RegisterOrganizationRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RegisterOrganizationAsync(request, ipAddress);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("Organization registered: {Email} from IP {IP}", request.Email, ipAddress);

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// Call this endpoint when access token is expired to get a new access token using the refresh token stored in HTTP-only cookie
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // read refresh token from HTTP-only cookie
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("Invalid refresh token");

            var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

            if (!result.IsSuccess)
                return Unauthorized(new { message = result.Message, errors = result.Errors });

            // Store new refresh token in HTTP-only cookie
            SetRefreshTokenCookie(result.Data!.RefreshToken);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = new
                {
                    accessToken = result?.Data?.AccessToken,
                    accessTokenExpiresAt = result?.Data?.AccessTokenExpiresAt,
                    user = result?.Data?.User
                }
            });
        }

        /// <summary>
        /// Logout and revoke refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // read refresh token from HTTP-only cookie
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized();

            var result = await _authService.LogoutAsync(refreshToken, ipAddress);

            ClearRefreshTokenCookie();

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Revoke all refresh tokens for the current user
        /// Call this endpoint from user profile settings when user want to log out from all devices or when user change password to ensure all old tokens are invalidated immediately
        /// </summary>
        [HttpPost("revoke-all-tokens")]
        [Authorize]
        public async Task<IActionResult> RevokeAllTokens()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var result = await _authService.RevokeAllTokensAsync(userGuid); // Revoke all refresh tokens for the user in the database

            // Clear refresh token cookie
            ClearRefreshTokenCookie();

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Change password for the current user
        /// From settings in user profile, when user change password we need to revoke all refresh tokens to force re-login with new password
        /// When press on change password button route him to this endpoint
        /// After changing pasword, immediately log out the user and clear refresh token cookie to prevent unauthorized access with old tokens
        /// Can call logout endpont after changing password and remove method ClearRefreshTokenCookie() from this endpoint, but I prefer to do it here to ensure all tokens are revoked immediately after password change without relying on client to call logout
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var result = await _authService.ChangePasswordAsync(userGuid, request);

            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message, errors = result.Errors });

            // Clear refresh token cookie since all tokens are revoked
            ClearRefreshTokenCookie();

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId,
                    email,
                    roles,
                    claims = User.Claims.Select(c => new { c.Type, c.Value })
                }
            });
        }

        #region Check Token Endpoints
        /// <summary>
        /// Validate if the current access token is active and not expired
        /// </summary>
        [HttpGet("validate-token")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { success = false, message = "Invalid token" });

            return Ok(new
            {
                success = true,
                message = "Token is valid",
                data = new
                {
                    isValid = true,
                    userId,
                    email
                }
            });
        }


        /// <summary>
        /// Check token expiration without requiring authentication
        /// </summary>
        [HttpPost("check-token")]
        [AllowAnonymous]
        public IActionResult CheckToken([FromBody] string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                return BadRequest(new { success = false, message = "Access token is required" });

            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var token = tokenHandler.ReadJwtToken(accessToken);

                var isExpired = token.ValidTo < DateTime.UtcNow;
                var expiresAt = token.ValidTo;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        isExpired,
                        expiresAt,
                        isActive = !isExpired
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token");
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid token format"
                });
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Sets refresh token as HTTP-only secure cookie
        /// </summary>
        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Cannot be accessed by JavaScript
                Secure = false, // Set to false to allow deletion over HTTP during development
                SameSite = SameSiteMode.Strict, // CSRF protection
                Expires = DateTime.UtcNow.AddDays(7), // Match refresh token expiration
                IsEssential = true,
                Path = "/" // Available to entire application
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        /// <summary>
        /// Clears the refresh token cookie
        /// </summary>
        private void ClearRefreshTokenCookie()
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to false to allow deletion over HTTP during development
                //SameSite = SameSiteMode.Lax,
                Path = "/"
            });
        }

        #endregion

        #region Forgot Password Flow

        /// <summary>
        /// Request password reset - sends OTP to email (Step 1)
        /// Enter email and press on forgot password button, route him to this endpoint to send OTP code to his email
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("Password reset requested for {Email}", request.Email);

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        /// <summary>
        /// Verify password reset OTP code (Step 2)
        /// When user receive OTP code in email, he will enter it in the app and press on verify button, route him to this endpoint to verify the code before allowing him to reset password
        /// Store Otp and email in temporary storage (like in-memory cache or database) with short expiration time (e.g. 15 minutes) after successful verification, then allow user to reset password using the verified Otp code and email in the next step
        /// </summary>
        [HttpPost("verify-reset-code")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyPasswordResetCode([FromBody] VerifyPasswordResetCodeRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var result = await _authService.VerifyPasswordResetCodeAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("Password reset code verified for {Email}", request.Email);

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        /// <summary>
        /// Reset password with OTP code (Step 3)
        /// Retry otp and email from temporary storage to ensure the code is valid and not expired, then allow user to reset password
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("Password reset successful for {Email}", request.Email);

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        /// <summary>
        /// Resend password reset OTP code
        /// Call this endpoint when user want to resend the OTP code to his email if he didn't receive it or if the code is expired, route him to this endpoint to resend the code using the same forgot password logic since it will generate a new code and invalidate the old one
        /// </summary>
        [HttpPost("resend-reset-code")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendPasswordResetCode([FromBody] ForgotPasswordRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(request);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("Password reset code resent to {Email}", request.Email);

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        #endregion

        #region Google Authenticator 2FA

        /// <summary>
        /// Step 1: Enable 2FA - Generate QR code for Google Authenticator
        /// </summary>
        /// <remarks>
        /// User must be logged in and provide their current password.
        /// Returns a QR code URL and manual entry key.
        /// 
        /// **Flow:**
        /// 1. User goes to Settings → Security → Enable 2FA
        /// 2. User enters password
        /// 3. Backend generates QR code
        /// 4. User scans QR code with Google Authenticator app
        /// 5. User proceeds to Step 2 (verify-setup)
        /// </remarks>
        [HttpPost("2fa/enable")]
        [Authorize]
        [ProducesResponseType(typeof(Enable2FAResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Enable2FAAuthenticator([FromBody] Enable2FARequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var result = await _authService.Enable2FAAuthenticatorAsync(userGuid, request);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("2FA setup initiated for user {UserId}", userGuid);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        /// <summary>
        /// Step 2: Verify 2FA setup - Confirm code from Google Authenticator
        /// </summary>
        /// <remarks>
        /// After scanning QR code, user must enter the 6-digit code shown in their authenticator app.
        /// This confirms the setup is working correctly before enabling 2FA.
        /// 
        /// **Flow:**
        /// 1. User enters 6-digit code from Google Authenticator
        /// 2. Backend validates the code
        /// 3. If valid, 2FA is enabled
        /// 4. User should then generate backup codes
        /// </remarks>
        [HttpPost("2fa/verify-setup")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Verify2FASetup([FromBody] Verify2FASetupRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var result = await _authService.Verify2FASetupAsync(userGuid, request);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("2FA enabled successfully for user {UserId}", userGuid);

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Disable 2FA - Requires password and current authenticator code
        /// </summary>
        /// <remarks>
        /// User must provide both their password and a valid authenticator code to disable 2FA.
        /// This ensures only the account owner can disable 2FA.
        /// 
        /// **Security:**
        /// - Requires password verification
        /// - Requires valid authenticator code
        /// - Deletes all backup codes
        /// - Removes authenticator key
        /// </remarks>
        [HttpPost("2fa/disable")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Disable2FAAuthenticator([FromBody] Disable2FARequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var result = await _authService.Disable2FAAuthenticatorAsync(userGuid, request);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("2FA disabled for user {UserId}", userGuid);

            return Ok(new { success = true, message = result.Message });
        }

        /// <summary>
        /// Get current 2FA status for the authenticated user
        /// </summary>
        /// <remarks>
        /// Returns information about the user's 2FA configuration including:
        /// - Whether 2FA is enabled
        /// - Number of remaining backup codes
        /// - When 2FA was enabled
        /// 
        /// **Use Case:**
        /// Frontend calls this to display 2FA settings on the security page
        /// </remarks>
        [HttpGet("2fa/status")]
        [Authorize]
        [ProducesResponseType(typeof(TwoFactorStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Get2FAStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var result = await _authService.Get2FAStatusAsync(userGuid);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, data = result.Data });
        }

        /// <summary>
        /// Verify 2FA code during login (Step 2 of login flow)
        /// </summary>
        /// <remarks>
        /// After successful email/password authentication, if 2FA is enabled, 
        /// user must enter the 6-digit code from their authenticator app.
        /// 
        /// **Flow:**
        /// 1. User logs in with email/password
        /// 2. Backend returns requires2FA: true
        /// 3. Frontend shows 2FA code input
        /// 4. User enters code from Google Authenticator
        /// 5. This endpoint validates the code
        /// 6. Returns access token + refresh token on success
        /// 
        /// **Backup Codes:**
        /// User can also enter an 8-character backup code instead of authenticator code
        /// </remarks>
        [HttpPost("2fa/verify-login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Verify2FALogin([FromBody] Verify2FALoginRequest request)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.Verify2FALoginAsync(request, ipAddress);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            // Store refresh token in HTTP-only cookie
            SetRefreshTokenCookie(result.Data!.RefreshToken);

            _logger.LogInformation("2FA verification successful for {Email} from IP {IP}", request.Email, ipAddress);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = new
                {
                    accessToken = result.Data.AccessToken,
                    accessTokenExpiresAt = result.Data.AccessTokenExpiresAt,
                    user = result.Data.User
                }
            });
        }

        /// <summary>
        /// Generate 10 backup codes for 2FA recovery
        /// </summary>
        /// <remarks>
        /// Backup codes allow users to access their account if they lose their authenticator device.
        /// - Generates 10 codes, each 8 characters long
        /// - Each code can only be used once
        /// - Old codes are deleted when generating new ones
        /// 
        /// **Important:**
        /// User should save these codes in a safe place (password manager, printed paper, etc.)
        /// 
        /// **Use Case:**
        /// - After enabling 2FA (recommended)
        /// - When user loses access to authenticator
        /// - When user gets a new phone
        /// </remarks>
        [HttpPost("2fa/backup-codes")]
        [Authorize]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GenerateBackupCodes()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var result = await _authService.GenerateBackupCodesAsync(userGuid);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            _logger.LogInformation("Backup codes generated for user {UserId}", userGuid);

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        #endregion
    }
}