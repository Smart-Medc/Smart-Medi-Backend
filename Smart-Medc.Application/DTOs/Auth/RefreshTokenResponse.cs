using Smart_Medc.Domain.Entities.Identity;

namespace Smart_Medc.Application.DTOs.Auth
{
    public class RefreshTokenResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByIp { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
