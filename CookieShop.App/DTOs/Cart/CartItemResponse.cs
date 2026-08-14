namespace CookieShop.App.DTOs.Cart;

public record CartItemResponse(
    Guid ProductId,
    string ProductName,
    string ImageUrl,
    long OriginalPrice,
    long UnitPrice,
    int Quantity,
    long LineTotal
);
