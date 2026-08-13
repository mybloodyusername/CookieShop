using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.City;

public record UpdateCityRequest(
    [Required] Guid Id,
    [Required, MaxLength(64)] string Name,
    [Required] Guid ProvinceId
);