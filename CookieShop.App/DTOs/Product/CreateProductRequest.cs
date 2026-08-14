using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Product;

public record CreateProductRequest(
    [Required, MaxLength(100)] string Name,
    [Required, Range(typeof(long), "1", "100000000000")] long Price,
    bool IsOnSale,
    [Range(typeof(long), "1", "100000000000")] long? SalePrice,
    [Required, Range(0, 100_000)] int StockQuantity,
    bool IsAvailable,
    [Required] Guid CategoryId,
    [MaxLength(500)] string Description = "",
    [MaxLength(512)] string ImageUrl = ""
);
