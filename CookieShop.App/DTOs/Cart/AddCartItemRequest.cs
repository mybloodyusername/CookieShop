using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Cart;

public record AddCartItemRequest(
    [Required] Guid ProductId,
    [Required, Range(1, 999)] int Quantity
);
