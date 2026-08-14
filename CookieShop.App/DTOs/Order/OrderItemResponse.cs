namespace CookieShop.App.DTOs.Order;

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    long UnitPrice,
    int Quantity,
    long LineTotal
);
