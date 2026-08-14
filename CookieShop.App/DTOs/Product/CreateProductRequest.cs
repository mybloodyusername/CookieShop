using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Product;

public record CreateProductRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string Description,
    [Required, Range(0.01, 1_000_000)] decimal Price,
    bool IsOnSale,
    [Range(0.01, 1_000_000)] decimal? SalePrice,
    [MaxLength(512)] string ImageUrl,
    [Required, Range(0, 100_000)] int StockQuantity,
    bool IsAvailable,
    [Required] Guid CategoryId
);
