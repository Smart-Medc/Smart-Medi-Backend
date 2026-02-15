using Microsoft.AspNetCore.Authorization;
using Smart_Medc.Application.Common.Auth.Requirements;
using Smart_Medc.Application.Common.Constants;
using Smart_Medc.Domain.Enums;

namespace Smart_Medc.Infrastructure.Services.Auth.Handlers
{
    public class VerifiedOrganizationHandler : AuthorizationHandler<VerifiedOrganizationRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            VerifiedOrganizationRequirement requirement)
        {
            var verificationStatusClaim = context.User.FindFirst(AppClaims.VerificationStatus);

            if (verificationStatusClaim != null &&
                Enum.TryParse<VerificationStatus>(verificationStatusClaim.Value, out var status) &&
                status == VerificationStatus.Verified)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}