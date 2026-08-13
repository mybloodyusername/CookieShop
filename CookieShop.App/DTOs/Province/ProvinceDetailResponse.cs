using CookieShop.App.DTOs.City;

namespace CookieShop.App.DTOs.Province;

public record ProvinceDetailResponse(
    string Id,
    string Name,
    ICollection<CityResponse> Cities
);