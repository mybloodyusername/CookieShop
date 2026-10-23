namespace CookieShop.App.DTOs.Cart;

public record CartResponse(
    Guid Id,
    IReadOnlyCollection<CartItemResponse> Items,
    decimal TotalAmount
)
{
    public string Currency => CookieShop.Domain.Constants.StoreCurrency.Code;
}
