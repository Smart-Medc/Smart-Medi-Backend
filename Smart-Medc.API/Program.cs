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
using System.Text.Json.Serialization;

namespace Smart_Medc.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers(option => option.Filters.Add<GlobalValidationFilter>())
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // Register Infrastructure Services (DbContext, Identity, Repositories)
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Register Application Services (Business Logic Services)
            builder.Services.AddApplicationServices(builder.Configuration);

            // Register Api Services
            builder.Services.AddApiServices(builder.Configuration);

            // Disable automatic validation for asp.net core and enabled GlobalValidationFilter instead
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            // Configure OpenAPI/Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Smart Medi API",
                    Version = "v1",
                    Description = "Comprehensive medical management system API",
                    Contact = new OpenApiContact
                    {
                        Name = "Smart Medi Team",
                        Email = "amaryasser.dev@gmail.com"
                    }
                });

                // Add JWT Authentication to Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
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

            // Add CORS policy (configure as needed)
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
                            "https://admin-dashboard-sigma-one-74.vercel.app" // for production admin dashboard
                          )
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });


            var app = builder.Build();

            // Apply pending migrations (recreates tables if missing, safe to run every startup)
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

                // Seed roles after tables exist
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var roleLogger = services.GetRequiredService<ILogger<Program>>();
                await RoleSeeder.SeedRolesAsync(roleManager, roleLogger);
            }

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Medi API v1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI, but fucking not work. Fix it later.
                });
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(builder.Environment.ContentRootPath, "uploads")),
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

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapControllers();

            // Register Notification Module dendencies and SignalR hub

            app.MapHub<NotificationHub>("/hubs/notifications");

            if (app.Environment.IsDevelopment())
            {
                app.UseHangfireDashboard("/hangfire");
            }

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