using CookieShop.App.DTOs.Category;

namespace CookieShop.App.DTOs.Product;

public record ProductDetailResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsOnSale,
    decimal? SalePrice,
    string ImageUrl,
    int StockQuantity,
    bool IsAvailable,
    Guid CategoryId,
    CategoryResponse Category,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
