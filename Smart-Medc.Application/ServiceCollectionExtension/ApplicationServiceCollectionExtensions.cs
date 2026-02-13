using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Application.Services;

namespace Smart_Medc.Application.ServiceCollectionExtension
{
    public static class ApplicationServiceCollectionExtensions
    {
        // This extension method is only used to register infrastructure services.
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<IMedicalRecordService, MedicalRecordService>();
            services.AddScoped<IMedicationService, MedicationService>();
            services.AddScoped<IJournalService, JournalService>();
            return services;
        }
    }
}
