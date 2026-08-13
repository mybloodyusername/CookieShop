using CookieShop.App.DTOs.Province;

namespace CookieShop.App.DTOs.City;

public record CityDetailResponse(
    string Id,
    string Name,
    ProvinceResponse Province,
    string ProvinceId
);