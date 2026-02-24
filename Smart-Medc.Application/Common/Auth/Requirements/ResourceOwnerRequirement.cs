using Microsoft.AspNetCore.Authorization;

namespace Smart_Medc.Application.Common.Authorization.Requirements
{
    public class ResourceOwnerRequirement : IAuthorizationRequirement
    {
        public string ResourceIdParameterName { get; }

        public ResourceOwnerRequirement(string resourceIdParameterName = "id")
        {
            ResourceIdParameterName = resourceIdParameterName;
        }
    }
}