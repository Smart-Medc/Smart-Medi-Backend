using Amazon.S3;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Interfaces.Repositories;
using Smart_Medc.Domain.Interfaces.Repositories.AI;
using Smart_Medc.Domain.Interfaces.Repositories.Appointments;
using Smart_Medc.Domain.Interfaces.Repositories.DataSharing;
using Smart_Medc.Domain.Interfaces.Repositories.Identity;
using Smart_Medc.Domain.Interfaces.Repositories.Notifications;
using Smart_Medc.Domain.Interfaces.Repositories.Organizations;
using Smart_Medc.Domain.Interfaces.Repositories.Patients;
using Smart_Medc.Infrastructure.Persistence;
using Smart_Medc.Infrastructure.Persistence.Repositories;
using Smart_Medc.Infrastructure.Persistence.Repositories.AI;
using Smart_Medc.Infrastructure.Persistence.Repositories.Appointments;
using Smart_Medc.Infrastructure.Persistence.Repositories.DataSharing;
using Smart_Medc.Infrastructure.Persistence.Repositories.Identity;
using Smart_Medc.Infrastructure.Persistence.Repositories.Notifications;
using Smart_Medc.Infrastructure.Persistence.Repositories.Organizations;
using Smart_Medc.Infrastructure.Persistence.Repositories.Patients;
using Smart_Medc.Infrastructure.Services.Storage;

namespace Smart_Medc.Infrastructure.ServiceCollectionExtension
{
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Registers infrastructure services including DbContext, Identity, and Repositories
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext(configuration);

            services.AddDbHealtchCheck(configuration);

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            services.AddIdentityAndAuthRepositories();

            services.AddPatientRepositories();

            services.AddDataSharingRepositories();

            services.AddOrganizationRepositories();

            services.AddAppointmentRepositories();

            services.AddNotificationRepositories();

            services.AddAIChatRepositories();

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var accountId = configuration["CloudflareR2:AccountId"];
                var accessKeyId = configuration["CloudflareR2:AccessKeyId"];
                var secretAccessKey = configuration["CloudflareR2:SecretAccessKey"];

                var config = new AmazonS3Config
                {
                    ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                    ForcePathStyle = true,
                };

                return new AmazonS3Client(accessKeyId, secretAccessKey, config);
            });

            services.AddScoped<IFileStorageService, CloudflareR2StorageService>();

            return services;
        }

        private static IServiceCollection AddDbContext(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("AliReda"),
                    sqlOptions => sqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)
                        .CommandTimeout(30)
                ));

            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                // User settings
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            })
           .AddEntityFrameworkStores<ApplicationDbContext>()
           .AddDefaultTokenProviders();

            return services;
        }

        private static IServiceCollection AddIdentityAndAuthRepositories(this IServiceCollection services)
        {
            services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IOtpVerificationRepository, OtpVerificationRepository>();
            services.AddScoped<IBackupCodeRepository, BackupCodeRepository>();
            services.AddScoped<IUserNotificationPreferenceRepository, UserNotificationPreferenceRepository>();
            return services;
        }

        private static IServiceCollection AddPatientRepositories(this IServiceCollection services)
        {
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
            services.AddScoped<IMedicalRecordDocumentRepository, MedicalRecordDocumentRepository>();
            services.AddScoped<IMedicationRepository, MedicationRepository>();
            services.AddScoped<IMedicationReminderRepository, MedicationReminderRepository>();
            services.AddScoped<IMedicationAdherenceLogRepository, MedicationAdherenceLogRepository>();
            services.AddScoped<IJournalEntryRepository, JournalEntryRepository>();
            services.AddScoped<IJournalEntryTagRepository, JournalEntryTagRepository>();
            return services;
        }

        private static IServiceCollection AddDataSharingRepositories(this IServiceCollection services)
        {
            services.AddScoped<IDataShareCodeRepository, DataShareCodeRepository>();
            services.AddScoped<IDataShareRecordAccessRepository, DataShareRecordAccessRepository>();
            services.AddScoped<IDataShareAccessLogRepository, DataShareAccessLogRepository>();
            return services;
        }

        private static IServiceCollection AddOrganizationRepositories(this IServiceCollection services)
        {
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IOrganizationSpecializationRepository, OrganizationSpecializationRepository>();
            services.AddScoped<ISpecializationRepository, SpecializationRepository>();
            services.AddScoped<IOrganizationDocumentRepository, OrganizationDocumentRepository>();
            services.AddScoped<IOrganizationOperatingHoursRepository, OrganizationOperatingHoursRepository>();
            services.AddScoped<IOrganizationAvailabilitySlotRepository, OrganizationAvailabilitySlotRepository>();
            services.AddScoped<IOrganizationAvailabilityExceptionRepository, OrganizationAvailabilityExceptionRepository>();
            services.AddScoped<IConsultationFeeRepository, ConsultationFeeRepository>();
            services.AddScoped<IOrganizationPhotoRepository, OrganizationPhotoRepository>();
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            return services;
        }

        private static IServiceCollection AddAppointmentRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<IAppointmentStatusHistoryRepository, AppointmentStatusHistoryRepository>();
            services.AddScoped<IAppointmentReminderRepository, AppointmentReminderRepository>();
            return services;
        }

        private static IServiceCollection AddNotificationRepositories(this IServiceCollection services)
        {
            services.AddScoped<IPatientNotificationRepository, PatientNotificationRepository>();
            services.AddScoped<IOrganizationNotificationRepository, OrganizationNotificationRepository>();
            return services;
        }

        private static IServiceCollection AddAIChatRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAIChatSessionRepository, AIChatSessionRepository>();
            services.AddScoped<IAIChatMessageRepository, AIChatMessageRepository>();
            services.AddScoped<IAIChatMessageAttachmentRepository, AIChatMessageAttachmentRepository>();
            return services;
        }

        private static IServiceCollection AddDbHealtchCheck(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddSqlServer(
                    configuration.GetConnectionString("cs") ?? throw new InvalidOperationException("Invalid connection string"),
                    name: "SQL Server",
                    healthQuery: "SELECT 1;",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "db", "sql", "sqlserver" });


            return services;
        }
    }
}