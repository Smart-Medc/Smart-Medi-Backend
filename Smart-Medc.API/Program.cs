using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Smart_Medc.Application.ServiceCollectionExtension;
using Smart_Medc.Infrastructure.ServiceCollectionExtension;

namespace Smart_Medc.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            // Register Infrastructure Services (DbContext, Identity, Repositories)
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Register Application Services (Business Logic Services)
            builder.Services.AddApplicationServices(builder.Configuration);

            // Configure OpenAPI/Swagger
            builder.Services.AddOpenApi();

            // Add CORS policy (configure as needed)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapControllers();

            app.Run();
        }
    }
}