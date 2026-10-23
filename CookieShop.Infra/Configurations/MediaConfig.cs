using CookieShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CookieShop.Infra.Configurations;

public class MediaConfig : IEntityTypeConfiguration<MediaImage>, IEntityTypeConfiguration<ProductImage>, IEntityTypeConfiguration<ImageBlob>
{
    public void Configure(EntityTypeBuilder<MediaImage> builder)
    {
        builder.HasIndex(x => new { x.CreatedAt, x.Id });
        builder.HasIndex(x => x.Album);
    }
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.ImageId });
        builder.HasIndex(x => new { x.ProductId, x.SortOrder });
        builder.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Image).WithMany(x => x.Products).HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<ImageBlob> builder)
    {
        builder.Property(x => x.Data).HasColumnType("bytea").IsRequired();
        builder.HasOne<MediaImage>().WithOne().HasForeignKey<ImageBlob>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
    }
}
