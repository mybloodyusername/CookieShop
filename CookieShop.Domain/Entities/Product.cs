using System.ComponentModel.DataAnnotations;

namespace CookieShop.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)] public required string Name { get; set; }

    [MaxLength(500)] public string Description { get; set; } = string.Empty;

    public long Price { get; set; }

    public bool IsOnSale { get; set; }

    public long? SalePrice { get; set; }

    [MaxLength(512)] public string ImageUrl { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public bool IsAvailable { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
