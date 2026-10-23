using System.ComponentModel.DataAnnotations;
namespace CookieShop.Domain.Entities;

// Metadata stays in PostgreSQL even when bytes move to object storage.
public class MediaImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string FileName { get; set; } = "image.webp";
    [MaxLength(500)] public string Alt { get; set; } = "";
    [MaxLength(100)] public string? Album { get; set; }
    [Required, MaxLength(32)] public string StorageProvider { get; set; } = "Postgres";
    [Required, MaxLength(256)] public string StorageKey { get; set; } = "";
    [Required, MaxLength(64)] public string Sha256 { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public long ByteLength { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ProductImage> Products { get; set; } = new List<ProductImage>();
}
