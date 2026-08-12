using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CookieShop.Infra.Configurations;

public class AddressConfig : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasOne(e => e.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Province)
            .WithMany()
            .HasForeignKey(e => e.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.City)
            .WithMany()
            .HasForeignKey(e => e.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
