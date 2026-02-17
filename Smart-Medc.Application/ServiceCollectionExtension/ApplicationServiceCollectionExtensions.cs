using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Smart_Medc.Application.Implementation;
using Smart_Medc.Application.Interfaces;

namespace Smart_Medc.Application.ServiceCollectionExtension
{
    public static class ApplicationServiceCollectionExtensions
    {
        // This extension method is only used to register infrastructure services.
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddAutoMapperConfig();

            services.AddAppointmentServices();

            services.AddOrganizationServices();

            services.AddDataSharingServices();

            return services;
        }

        private static IServiceCollection AddAutoMapperConfig(this IServiceCollection services)
        {
            // Scanning the specific assembly containing profiles
            services.AddAutoMapper(typeof(ApplicationServiceCollectionExtensions).Assembly);
            return services;
        }

        private static IServiceCollection AddAppointmentServices(this IServiceCollection services)
        {
            services.AddScoped<IAppointmentService, AppointmentService>();
            return services;
        }

        private static IServiceCollection AddOrganizationServices(this IServiceCollection services)
        {
            services.AddScoped<IOrganizationService, OrganizationService>();
            return services;
        }

        private static IServiceCollection AddDataSharingServices(this IServiceCollection services)
        {
            services.AddScoped<IDataSharingService, DataSharingService>();
            return services;
        }
    }
}
