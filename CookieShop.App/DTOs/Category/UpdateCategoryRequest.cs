using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Category;

public record UpdateCategoryRequest(
    [Required] Guid Id,
    [Required, MaxLength(64)] string Name
);
