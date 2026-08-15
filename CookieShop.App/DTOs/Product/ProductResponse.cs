namespace CookieShop.App.DTOs.Product;

public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool IsOnSale,
    decimal? SalePrice,
    string ImageUrl,
    int StockQuantity,
    bool IsAvailable,
    Guid CategoryId
);
