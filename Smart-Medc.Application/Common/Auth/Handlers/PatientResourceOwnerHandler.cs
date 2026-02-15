using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smart_Medc.Application.Common.Authorization.Requirements;
using Smart_Medc.Application.Common.Constants;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Appli.Services.Auth.Handlers
{
    public class PatientResourceOwnerHandler : AuthorizationHandler<ResourceOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;

        public PatientResourceOwnerHandler(
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork unitOfWork)
        {
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            var patientIdClaim = context.User.FindFirst(AppClaims.PatientId)?.Value;
            if (string.IsNullOrEmpty(patientIdClaim))
                return;

            if (!Guid.TryParse(patientIdClaim, out var patientId))
                return;

            // Get resource ID from route
            var routeData = httpContext.GetRouteData();
            if (routeData == null)
                return;

            if (!routeData.Values.TryGetValue(requirement.ResourceIdParameterName, out var resourceIdObj))
                return;

            if (!Guid.TryParse(resourceIdObj?.ToString(), out var resourceId))
                return;

            // Check ownership (this is a simplified check - expand based on resource type)
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId);
            if (patient != null && patient.Id == resourceId)
            {
                context.Succeed(requirement);
            }
        }
    }
}