using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CookieShop.Infra.Data;

public static class SeedData
{
    public static async Task Initialize(
        ILogger<CookieShopDbContext> logger,
        IServiceProvider serviceProvider,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        using var scope = serviceProvider.CreateScope();

        var context = serviceProvider.GetRequiredService<CookieShopDbContext>();

        foreach (var role in UserRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                logger.LogInformation("Role created successfully: {RoleName}", role);
            }
            else
            {
                logger.LogInformation("Role already exists: {RoleName}", role);
            }
        }

        var adminEmail = "admin@cookieshop.com";
        var adminPhone = "09912772092";
        var adminPassword = "Admin@123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Email = adminEmail,
                UserName = adminPhone,
                PhoneNumber = adminPhone,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRole.Admin);
                logger.LogInformation("Admin created successfully!");
            }
            else
            {
                throw new Exception("Failed to create admin user: " +
                                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin already exists!");
        }

        await context.SaveChangesAsync();
    }
}