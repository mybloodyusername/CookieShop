using CookieShop.Domain.Entities;
using CookieShop.Infra.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Data;

public class CookieShopDbContext(DbContextOptions<CookieShopDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new UserConfig());
        builder.ApplyConfiguration(new AddressConfig());
        builder.ApplyConfiguration(new CategoryConfig());
        builder.ApplyConfiguration(new ProductConfig());
        builder.ApplyConfiguration(new CartConfig());
        builder.ApplyConfiguration(new CartItemConfig());
        builder.ApplyConfiguration(new OrderConfig());
        builder.ApplyConfiguration(new OrderItemConfig());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        // TODO: 
        return base.SaveChangesAsync(cancellationToken);
    }
}