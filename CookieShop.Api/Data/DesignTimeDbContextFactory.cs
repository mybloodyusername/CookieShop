using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CookieShop.Api.Data;

// Migration tooling must not start the web host, seed accounts or require JWT secrets.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CookieShopDbContext>
{
    public CookieShopDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=cookie_shop";
        return new CookieShopDbContext(new DbContextOptionsBuilder<CookieShopDbContext>()
            .UseNpgsql(connection).Options);
    }
}
