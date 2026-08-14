using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Category;

public record CreateCategoryRequest(
    [Required, MaxLength(64)] string Name
);
