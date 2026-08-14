namespace CookieShop.App.DTOs.Cart;

public record CartResponse(
    Guid Id,
    IReadOnlyCollection<CartItemResponse> Items,
    long TotalAmount
);
