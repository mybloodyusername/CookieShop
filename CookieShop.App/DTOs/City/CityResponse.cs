namespace CookieShop.App.DTOs.City;

public record CityResponse(
    Guid Id,
    string Name,
    string ProvinceId
);