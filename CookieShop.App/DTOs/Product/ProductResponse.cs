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
)
{
    public string Currency => CookieShop.Domain.Constants.StoreCurrency.Code;
    public IReadOnlyCollection<CookieShop.App.DTOs.Media.ProductImageResponse> Images { get; init; } = [];
}
