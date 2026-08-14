using System.ComponentModel.DataAnnotations;

namespace CookieShop.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [Required, MaxLength(100)] public required string ProductName { get; set; }

    public long UnitPrice { get; set; }

    public int Quantity { get; set; }
}
