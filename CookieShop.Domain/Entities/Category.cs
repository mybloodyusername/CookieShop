using System.ComponentModel.DataAnnotations;

namespace CookieShop.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }

    [Required, MaxLength(64)] public required string Name { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
