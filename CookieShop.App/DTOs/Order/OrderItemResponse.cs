namespace CookieShop.App.DTOs.Order;

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);
