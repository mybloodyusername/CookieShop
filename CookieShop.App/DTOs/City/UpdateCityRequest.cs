using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.City;

public record UpdateCityRequest(
    [Required] string Id,
    [Required, MaxLength(64)] string Name,
    [Required] string ProvinceId
);