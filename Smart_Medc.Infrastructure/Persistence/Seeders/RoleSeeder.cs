using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Smart_Medc.Infrastructure.Persistence.Seeders
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger logger)
        {
            try
            {
                var allRoles = new[] { "Admin", "Patient", "Organization" };
                foreach (var roleName in allRoles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var role = new IdentityRole<Guid>(roleName);
                        var result = await roleManager.CreateAsync(role);

                        if (result.Succeeded)
                        {
                            logger.LogInformation($"Role '{roleName}' created successfully");
                        }
                        else
                        {
                            logger.LogError($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding roles");
            }
        }
    }
}