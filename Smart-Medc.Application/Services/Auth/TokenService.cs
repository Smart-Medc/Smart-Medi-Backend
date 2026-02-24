using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Smart_Medc.Application.Common.Constants;
using Smart_Medc.Application.Configuration;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Interfaces.Repositories;
using Smart_Medc.Domain.Interfaces.Services.Auth;

namespace Smart_Medc.Infrastructure.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(AppClaims.UserId, user.Id.ToString()),
                new(AppClaims.Email, user.Email ?? string.Empty),
                new(AppClaims.UserType, user.UserType.ToString()),
                new(AppClaims.FullName, user.FullName),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            // Add role-based claims
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // Add user-specific claims based on UserType
            if (user.UserType == Domain.Enums.UserType.Patient && user.Patient != null)
            {
                claims.Add(new Claim(AppClaims.PatientId, user.Patient.Id.ToString()));
                claims.Add(new Claim(AppClaims.CanAccessMedicalRecords, "true"));
            }
            else if (user.UserType == Domain.Enums.UserType.Organization && user.Organization != null)
            {
                claims.Add(new Claim(AppClaims.OrganizationId, user.Organization.Id.ToString()));
                claims.Add(new Claim(AppClaims.OrganizationName, user.Organization.Name));
                claims.Add(new Claim(AppClaims.VerificationStatus, user.Organization.VerificationStatus.ToString()));
                claims.Add(new Claim(AppClaims.CanManageAppointments, "true"));
            }
            else if (user.UserType == Domain.Enums.UserType.Admin)
            {
                claims.Add(new Claim(AppClaims.CanVerifyOrganizations, "true"));
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<string> GenerateRefreshTokenAsync()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Task.FromResult(Convert.ToBase64String(randomNumber));
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user, string token, string? ipAddress = null)
        {
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<bool> ValidateRefreshTokenAsync(string token)
        {
            var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token);
            return refreshToken != null && refreshToken.IsActive;
        }

        public async Task RevokeRefreshTokenAsync(string token, string? ipAddress = null, string? reason = null)
        {
            var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token);
            if (refreshToken == null || !refreshToken.IsActive)
                return;

            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReasonRevoked = reason ?? "Revoked without replacement";

            await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RevokeAllUserRefreshTokensAsync(Guid userId, string? reason = null)
        {
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}