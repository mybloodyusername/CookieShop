using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CookieShop.Infra.Extensions;

public static class HostExtension
{
    extension(IHost host)
    {
        public async Task InitializeDatabaseAsync()
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<CookieShopDbContext>>();
            var context = services.GetRequiredService<CookieShopDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            await context.Database.MigrateAsync();
            await SeedData.Initialize(logger, services, userManager, roleManager);
        }
    }
}