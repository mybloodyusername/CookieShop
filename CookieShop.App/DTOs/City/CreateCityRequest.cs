using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.City;

public record CreateCityRequest(
    [Required, MaxLength(64)] string Name,
    [Required] Guid ProvinceId
);