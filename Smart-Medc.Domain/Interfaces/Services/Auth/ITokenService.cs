using System.Security.Claims;
using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Domain.Interfaces.Services.Auth
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user, string token, string? ipAddress = null);
        Task<bool> ValidateRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token, string? ipAddress = null, string? reason = null);
        Task RevokeAllUserRefreshTokensAsync(Guid userId, string? reason = null);
    }
}