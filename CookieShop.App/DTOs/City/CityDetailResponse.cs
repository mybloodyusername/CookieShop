using CookieShop.App.DTOs.Province;

namespace CookieShop.App.DTOs.City;

public record CityDetailResponse(
    Guid Id,
    string Name,
    ProvinceResponse Province,
    Guid ProvinceId
);