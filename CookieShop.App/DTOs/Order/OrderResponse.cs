namespace CookieShop.App.DTOs.Order;

public record OrderResponse(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
)
{
    public string Currency => CookieShop.Domain.Constants.StoreCurrency.Code;
}
