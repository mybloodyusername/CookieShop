using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Order;

public record CreateOrderRequest(
    [Required] Guid AddressId,
    [MaxLength(256)] string? Note
);
