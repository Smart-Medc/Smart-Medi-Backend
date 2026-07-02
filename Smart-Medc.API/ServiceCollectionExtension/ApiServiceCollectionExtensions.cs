using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Smart_Medc.API.Hubs;
using Smart_Medc.Appli.Services.Auth.Handlers;
using Smart_Medc.Application.Common.Auth.Requirements;
using Smart_Medc.Application.Common.Constants;
using Smart_Medc.Application.Configuration;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Infrastructure.Services.Auth.Handlers;
using System.Text;

namespace Smart_Medc.API.ServiceCollectionExtension
{
    public static class ApiServiceCollectionExtensions
    {
        // This extension method is only used to register infrastructure services.
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddJwtAuthentication(configuration);
            services.AddAuthorizationPolicies();

            services.AddScoped<IAuthorizationHandler, VerifiedOrganizationHandler>();
            services.AddScoped<IAuthorizationHandler, PatientResourceOwnerHandler>();

            // Register HttpContextAccessor
            services.AddHttpContextAccessor();

            // Register SignalR configuration 
            services.AddSignalRConfiguration();

            // Register Hangfire configuration
            services.AddHangfireConfiguration(configuration);

            // Register Notification infrastructure services (e.g., INotificationHubPusher)
            services.AddNotificationInfrastructure();

            services.AddChatHubDispatcher();

            return services;
        }

        private static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind JWT settings
            var jwtSettings = new JwtSettings();
            configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            // Add JWT Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtSettings.ValidateIssuer,
                    ValidateAudience = jwtSettings.ValidateAudience,
                    ValidateLifetime = jwtSettings.ValidateLifetime,
                    ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Unauthorized",
                            message = "You are not authorized to access this resource"
                        });
                        return context.Response.WriteAsync(result);
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Forbidden",
                            message = "You do not have permission to access this resource"
                        });
                        return context.Response.WriteAsync(result);
                    },
                    OnMessageReceived = context => // Allow JWT to be passed via query string for SignalR hubs
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/hubs/notifications") ||
                             path.StartsWithSegments("/hubs/chat")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }


        private static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Role-based policies
                options.AddPolicy(AppPolicies.RequirePatientRole, policy =>
                    policy.RequireRole(AppRoles.Patient));

                options.AddPolicy(AppPolicies.RequireOrganizationRole, policy =>
                    policy.RequireRole(AppRoles.Organization));

                options.AddPolicy(AppPolicies.RequireAdminRole, policy =>
                    policy.RequireRole(AppRoles.Admin));

                // Custom policies
                options.AddPolicy(AppPolicies.RequireVerifiedOrganization, policy =>
                {
                    policy.RequireRole(AppRoles.Organization);
                    policy.Requirements.Add(new VerifiedOrganizationRequirement());
                });

                options.AddPolicy(AppPolicies.RequireEmailVerification, policy =>
                    policy.RequireClaim("email_verified", "true"));
            });

            return services;
        }

        private static IServiceCollection AddSignalRConfiguration(this IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.MaximumReceiveMessageSize = 32 * 1024;
            });
            return services;
        }

        private static IServiceCollection AddChatHubDispatcher(this IServiceCollection services)
        {
            services.AddScoped<IChatHubDispatcher, ChatHubDispatcher>();
            return services;
        }

        private static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("cs")!;

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    SchemaName = "notif_jobs",
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 2;
                options.Queues = new[] { "notifications", "default" };
            });

            return services;
        }

        private static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<INotificationHubPusher, NotificationHubPusher>();
            return services;
        }
    }
}
