namespace CookieShop.App.DTOs.Cart;

public record CartItemResponse(
    Guid ProductId,
    string ProductName,
    string ImageUrl,
    decimal OriginalPrice,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);
