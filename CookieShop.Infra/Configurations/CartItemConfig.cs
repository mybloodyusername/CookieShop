using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CookieShop.Infra.Configurations;

public class CartItemConfig : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_CartItem_Quantity", "\"Quantity\" BETWEEN 1 AND 1000");
        });
        builder.Property<uint>("Version").IsRowVersion();
        builder.HasIndex(e => new { e.CartId, e.ProductId }).IsUnique();

        builder.HasOne(e => e.Product)
            .WithMany(p => p.CartItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
