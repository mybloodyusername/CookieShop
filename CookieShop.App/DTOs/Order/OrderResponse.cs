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
    public string PaymentStatus { get; init; } = "Unpaid";
    public decimal PaidAmount { get; init; }
    public decimal AmountDue => Status is "Cancelled" or "Rejected" ? 0 : Math.Max(0, TotalAmount - PaidAmount);
    public bool PayLater { get; init; }
    public string Currency => CookieShop.Domain.Constants.StoreCurrency.Code;
}
