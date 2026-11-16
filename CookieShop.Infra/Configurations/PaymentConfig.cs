using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CookieShop.Infra.Configurations;

public class OrderPaymentConfig : IEntityTypeConfiguration<OrderPayment>
{
    public void Configure(EntityTypeBuilder<OrderPayment> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Source).HasMaxLength(32);
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FinancialEntry).WithMany().HasForeignKey(x => x.FinancialEntryId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_OrderPayment_Amount", "\"Amount\" > 0 AND \"Amount\" = trunc(\"Amount\")"));
    }
}
public class PaymentAttemptConfig : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> b)
    {
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Provider).HasMaxLength(32);
        b.Property(x => x.Authority).HasMaxLength(128);
        b.Property(x => x.TransactionId).HasMaxLength(128);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property<uint>("Version").IsRowVersion();
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Provider, x.Authority }).IsUnique();
        b.HasIndex(x => new { x.Provider, x.TransactionId }).IsUnique().HasFilter("\"TransactionId\" IS NOT NULL");
        b.ToTable(t => t.HasCheckConstraint("CK_PaymentAttempt_Amount", "\"Amount\" > 0 AND \"Amount\" = trunc(\"Amount\")"));
    }
}
