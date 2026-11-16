using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CookieShop.Infra.Configurations;

public class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Order_Total", "\"TotalAmount\" >= 0 AND \"TotalAmount\" = trunc(\"TotalAmount\")");
        });
        builder.Property(e => e.OrderNumber).IsRequired().HasMaxLength(15)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.HasIndex(e => e.OrderNumber).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("CK_Order_Number", "\"OrderNumber\" ~ '^[0-9]{8}-[0-9]{6}$'"));
        builder.Property<uint>("Version").IsRowVersion();
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.PaidAmount).HasPrecision(18, 2);
        builder.Property(e => e.RefundedAmount).HasPrecision(18, 2);
        builder.ToTable(t => t.HasCheckConstraint("CK_Order_RefundedAmount", "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"PaidAmount\" AND \"RefundedAmount\" = trunc(\"RefundedAmount\")"));
        builder.Property(e => e.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.ToTable(t => t.HasCheckConstraint("CK_Order_PaidAmount", "\"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"TotalAmount\" AND \"PaidAmount\" = trunc(\"PaidAmount\")"));
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Address)
            .WithMany()
            .HasForeignKey(e => e.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
