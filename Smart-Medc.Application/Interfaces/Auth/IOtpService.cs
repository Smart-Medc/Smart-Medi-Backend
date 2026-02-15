using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Application.Interfaces.Auth
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(Guid userId, OtpPurpose purpose, OtpDeliveryMethod deliveryMethod);
        Task<bool> ValidateOtpAsync(Guid userId, string code, OtpPurpose purpose);
        Task<bool> SendOtpAsync(ApplicationUser user, OtpPurpose purpose);
        Task InvalidateOtpAsync(Guid userId, OtpPurpose purpose);
    }
}