using CookieShop.App.DTOs.Address;

namespace CookieShop.App.DTOs.Order;

public record OrderDetailResponse(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    AddressResponse Address,
    IReadOnlyCollection<OrderItemResponse> Items
);
