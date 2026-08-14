using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Chocolate Chip" },
                new() { Name = "Oatmeal" },
                new() { Name = "Classic" },
                new() { Name = "Filled" }
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            logger.LogInformation("Categories seeded successfully!");
        }

        if (!await context.Products.AnyAsync())
        {
            var categories = await context.Categories.ToDictionaryAsync(c => c.Name);

            var products = new List<Product>
            {
                new()
                {
                    Name = "Classic Chocolate Chip",
                    Description = "The timeless favorite — crispy edges, gooey center.",
                    Price = 49_000, IsOnSale = true, SalePrice = 39_000,
                    ImageUrl = "https://images.example.com/cookies/classic-chocolate-chip.png",
                    StockQuantity = 50, IsAvailable = true,
                    CategoryId = categories["Chocolate Chip"].Id
                },
                new()
                {
                    Name = "Double Chocolate",
                    Description = "Double the cocoa for the serious chocolate lover.",
                    Price = 59_000, IsOnSale = true, SalePrice = 49_000,
                    ImageUrl = "https://images.example.com/cookies/double-chocolate.png",
                    StockQuantity = 40, IsAvailable = true,
                    CategoryId = categories["Chocolate Chip"].Id
                },
                new()
                {
                    Name = "Oatmeal Raisin",
                    Description = "Hearty oats, sweet raisins, a hint of cinnamon.",
                    Price = 39_000, IsOnSale = true, SalePrice = 29_000,
                    ImageUrl = "https://images.example.com/cookies/oatmeal-raisin.png",
                    StockQuantity = 35, IsAvailable = true,
                    CategoryId = categories["Oatmeal"].Id
                },
                new()
                {
                    Name = "Snickerdoodle",
                    Description = "Soft and chewy, rolled in cinnamon sugar.",
                    Price = 34_000,
                    ImageUrl = "https://images.example.com/cookies/snickerdoodle.png",
                    StockQuantity = 45, IsAvailable = true,
                    CategoryId = categories["Classic"].Id
                },
                new()
                {
                    Name = "Sugar Cookie",
                    Description = "Buttery, simple, and perfect with a cup of tea.",
                    Price = 29_000,
                    ImageUrl = "https://images.example.com/cookies/sugar-cookie.png",
                    StockQuantity = 60, IsAvailable = true,
                    CategoryId = categories["Classic"].Id
                },
                new()
                {
                    Name = "Peanut Butter Sandwich",
                    Description = "Creamy peanut butter filling between two soft cookies.",
                    Price = 44_000,
                    ImageUrl = "https://images.example.com/cookies/peanut-butter-sandwich.png",
                    StockQuantity = 30, IsAvailable = true,
                    CategoryId = categories["Filled"].Id
                }
            };
            await context.Products.AddRangeAsync(products);
            logger.LogInformation("Products seeded successfully!");
        }

        await context.SaveChangesAsync();
    }
}