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

    [MaxLength(64)] public string ShippingTitle { get; set; } = "";
    [MaxLength(256)] public string ShippingAddressLine1 { get; set; } = "";
    [MaxLength(256)] public string ShippingAddressLine2 { get; set; } = "";
    [MaxLength(16)] public string ShippingPhoneNumber { get; set; } = "";

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Whole Iranian toman (IRT), without fractional units.</summary>
    public decimal TotalAmount { get; set; }

    [MaxLength(256)] public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
