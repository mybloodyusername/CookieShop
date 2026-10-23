using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Cart;

public record AddCartItemRequest(
    [Required] Guid ProductId,
    [Required, Range(1, 1000)] int Quantity
);
