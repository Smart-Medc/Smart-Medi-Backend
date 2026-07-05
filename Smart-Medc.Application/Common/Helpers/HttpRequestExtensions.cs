using Microsoft.AspNetCore.Http;

namespace Smart_Medc.Application.Common.Helpers
{
    public static class HttpRequestExtensions
    {
        public static ClientType GetClientType(this HttpRequest request)
        {
            var value = request.Headers["X-Client-Type"].ToString();

            return value.Equals("Mobile", StringComparison.OrdinalIgnoreCase) ? ClientType.Mobile : ClientType.Web;
        }
    }
}
