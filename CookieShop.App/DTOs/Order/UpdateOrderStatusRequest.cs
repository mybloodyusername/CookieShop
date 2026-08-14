using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Order;

public record UpdateOrderStatusRequest(
    [Required] Guid OrderId,
    [Required] string Status
);
