using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Cart;

public record UpdateCartItemRequest(
    [Required] Guid ProductId,
    [Required, Range(1, 999)] int Quantity
);
