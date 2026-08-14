namespace CookieShop.App.DTOs.Product;

public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    long Price,
    bool IsOnSale,
    long? SalePrice,
    string ImageUrl,
    int StockQuantity,
    bool IsAvailable,
    Guid CategoryId
);
