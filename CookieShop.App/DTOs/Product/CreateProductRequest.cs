using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Product;

public record CreateProductRequest(
    [Required, MaxLength(100)] string Name,
    [Required, Range(1, 999_999_999_999d)] decimal Price,
    bool IsOnSale,
    [Range(1, 999_999_999_999d)] decimal? SalePrice,
    [Required, Range(0, 100_000)] int StockQuantity,
    bool IsAvailable,
    [Required] Guid CategoryId,
    [MaxLength(500)] string Description = "",
    [MaxLength(512)] string ImageUrl = ""
);
