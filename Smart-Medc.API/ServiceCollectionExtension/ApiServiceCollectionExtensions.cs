using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Smart_Medc.Appli.Services.Auth.Handlers;
using Smart_Medc.Application.Common.Auth.Requirements;
using Smart_Medc.Application.Common.Constants;
using Smart_Medc.Application.Configuration;
using Smart_Medc.Infrastructure.Services.Auth.Handlers;

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
    }
}
