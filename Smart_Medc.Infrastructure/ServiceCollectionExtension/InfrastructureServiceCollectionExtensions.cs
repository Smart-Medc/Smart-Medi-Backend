using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Smart_Medc.Infrastructure.ServiceCollectionExtension
{
    public static class InfrastructureServiceCollectionExtensions
    {
        // This extension method is only used to register infrastructure services.
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
