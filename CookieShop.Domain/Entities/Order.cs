using System.ComponentModel.DataAnnotations;
using CookieShop.Domain.Enums;

namespace CookieShop.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid AddressId { get; set; }
    public Address Address { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    [MaxLength(256)] public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
