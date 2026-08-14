namespace CookieShop.App.DTOs.Order;

public record OrderResponse(
    Guid Id,
    Guid UserId,
    string Status,
    long TotalAmount,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
