using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Application.Services;
using Smart_Medc.Application.Configuration;
using Smart_Medc.Application.Interfaces.Auth;
using Smart_Medc.Application.Interfaces.Services;
using Smart_Medc.Application.Interfaces.Services.Auth;
using Smart_Medc.Application.Interfaces.Storage;
using Smart_Medc.Application.Services.Auth;
using Smart_Medc.Application.Services.Storage;
using Smart_Medc.Domain.Interfaces.Services.Auth;
using Smart_Medc.Infrastructure.Services.Auth;
using Smart_Medc.Application.Services.Patient;
using Smart_Medc.Application.Services.Organization;
using Smart_Medc.Application.Services.DataSharing;

namespace Smart_Medc.Application.ServiceCollectionExtension
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register Auth Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Register Email Service
            services.Configure<EmailSettings>(options =>
                configuration.GetSection(EmailSettings.SectionName).Bind(options));
            services.AddScoped<IEmailService, EmailService>();

            // Register OTP Service
            services.AddScoped<IOtpService, OtpService>();

            // Register Google Authenticator Service
            services.AddScoped<IGoogleAuthenticatorService, GoogleAuthenticatorService>();

            // Register File Storage Service
            services.AddScoped<ILocalFileStorageService, LocalFileStorageService>();

            // Register document service
            services.AddScoped<OrganizationDocumentService>();

            // Register Admin Service
            services.AddScoped<IAdminService, AdminService>();

            services.AddAutoMapperConfig();
            services.AddPatientServices();

            services.AddAppointmentServices();

            services.AddOrganizationServices();

            services.AddDataSharingServices();

            return services;
        }
        private static IServiceCollection AddPatientServices(this IServiceCollection services)
        {
            services.AddScoped<IMedicalRecordService, MedicalRecordService>();
            services.AddScoped<IMedicationService, MedicationService>();
            services.AddScoped<IJournalService, JournalService>();
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
