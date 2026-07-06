using System.Text.Json.Serialization;
using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Smart_Medc.API.Filters;
using Smart_Medc.API.Hubs;
using Smart_Medc.API.ServiceCollectionExtension;
using Smart_Medc.Application.BackgroundJobs;
using Smart_Medc.Application.ServiceCollectionExtension;
using Smart_Medc.Infrastructure.Persistence;
using Smart_Medc.Infrastructure.Persistence.Seeders;
using Smart_Medc.Infrastructure.ServiceCollectionExtension;

namespace Smart_Medc.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers(option => option.Filters.Add<GlobalValidationFilter>())
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Infrastructure
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Application Services
            builder.Services.AddApplicationServices(builder.Configuration);

            // API Services
            builder.Services.AddApiServices(builder.Configuration);



            // Disable auto validation
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Smart Medi API",
                    Version = "v1",
                    Description = "Comprehensive medical management system API with AI chat",
                    Contact = new OpenApiContact
                    {
                        Name = "Smart Medi Team",
                        Email = "support@smartmedi.com"
                    }
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:3000",
                            "http://localhost:5173",
                            "http://localhost:5379",
                            "https://localhost:7278",
                            "https://localhost:7039",
                            "https://admin-dashboard-sigma-one-74.vercel.app", // for production admin dashboard
                            "https://smart-medi-frontend-zeta.vercel.app", // for production frontend
                            "https://localhost:8000"
                          )
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            // Migrations
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var dbContext = services.GetRequiredService<ApplicationDbContext>();
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to apply database migrations.");
                    throw;
                }

                // Seed roles
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var roleLogger = services.GetRequiredService<ILogger<Program>>();
                await RoleSeeder.SeedRolesAsync(roleManager, roleLogger);
            }

            // Middleware
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Medi API v1");
                });
                app.UseDeveloperExceptionPage();
            }

            // Static files
            var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
                RequestPath = "/uploads",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                    ctx.Context.Response.Headers.Append("X-Frame-Options", "DENY");
                }
            });

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            // Health checks
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapControllers();

            // SignalR Hubs
            app.MapHub<NotificationHub>("/hubs/notifications");
            app.MapHub<ChatHub>("/hubs/chat");

            // Hangfire Dashboard
            if (app.Environment.IsDevelopment())
            {
                app.UseHangfireDashboard("/hangfire");
            }

            // Recurring jobs
            using (var scope = app.Services.CreateScope())
            {
                var recurringJobs = scope.ServiceProvider
                    .GetRequiredService<IRecurringJobManager>();

                recurringJobs.AddOrUpdate<AppointmentReminderJob>(
                    "appointment-reminders",
                    job => job.ProcessAsync(CancellationToken.None),
                    "*/30 * * * *");

                recurringJobs.AddOrUpdate<MedicationReminderJob>(
                    "medication-reminders",
                    job => job.ProcessAsync(CancellationToken.None),
                    "*/5 * * * *");

                recurringJobs.AddOrUpdate<AutoRejectAppointmentJob>(
                    "auto-reject-appointments",
                    job => job.ProcessAsync(CancellationToken.None),
                    "0 * * * *");

                recurringJobs.AddOrUpdate<NotificationCleanupJob>(
                    "notification-cleanup",
                    job => job.ProcessAsync(CancellationToken.None),
                    "0 3 * * *");
            }

            app.Run();
        }
    }
}