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
    public string OrderNumber { get; init; } = "";
    public string PaymentStatus { get; init; } = "Unpaid";
    public decimal RefundedAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal AmountDue => Status is "Cancelled" or "Rejected" ? 0 : Math.Max(0, TotalAmount - PaidAmount);
    public bool MonthlyBilling { get; init; }
    public string Currency => CookieShop.Domain.Constants.StoreCurrency.Code;
}
