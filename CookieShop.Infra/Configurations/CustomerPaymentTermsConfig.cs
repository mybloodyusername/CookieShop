using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CookieShop.Infra.Configurations;
public class CustomerPaymentTermsConfig : IEntityTypeConfiguration<CustomerPaymentTerms>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentTerms> b)
    {
        b.HasKey(x => x.UserId);
        b.HasOne(x => x.User).WithOne().HasForeignKey<CustomerPaymentTerms>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.DebtLimit).HasPrecision(18, 2);
        b.Property<uint>("Version").IsRowVersion();
        b.ToTable(t => t.HasCheckConstraint("CK_PaymentTerms_Limit", "\"DebtLimit\" IS NULL OR (\"DebtLimit\" >= 0 AND \"DebtLimit\" = trunc(\"DebtLimit\"))"));
    }
}
