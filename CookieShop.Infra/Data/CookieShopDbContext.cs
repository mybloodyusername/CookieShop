using CookieShop.Domain.Entities;
using CookieShop.Infra.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Data;

public class CookieShopDbContext(DbContextOptions<CookieShopDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    
    // TODO:public DbSet<ENTITY> Entity => Set<Entity>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new UserConfig());
        builder.ApplyConfiguration(new AddressConfig());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        // TODO: 
        return base.SaveChangesAsync(cancellationToken);
    }
}