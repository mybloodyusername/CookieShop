using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CookieShop.Infra.Configurations;
public class FinancialEntryConfig : IEntityTypeConfiguration<FinancialEntry>
{
    public void Configure(EntityTypeBuilder<FinancialEntry> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Kind).HasMaxLength(32);
        b.Property(x => x.Reference).HasMaxLength(128);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OrderId).IsUnique().HasFilter("\"Kind\" = 'OrderCharge'");
        b.HasIndex(x => new { x.UserId, x.Reference }).IsUnique().HasFilter("\"Reference\" <> ''");
        b.ToTable(t => t.HasCheckConstraint("CK_FinancialEntry_Amount", "\"Amount\" <> 0 AND \"Amount\" = trunc(\"Amount\")"));
    }
}
