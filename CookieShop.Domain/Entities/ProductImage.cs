using System.ComponentModel.DataAnnotations;
namespace CookieShop.Domain.Entities;

public class ProductImage
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid ImageId { get; set; }
    public MediaImage Image { get; set; } = null!;
    // Null inherits the library alt; empty explicitly marks a decorative image.
    [MaxLength(500)] public string? Alt { get; set; }
    public int SortOrder { get; set; }
}
