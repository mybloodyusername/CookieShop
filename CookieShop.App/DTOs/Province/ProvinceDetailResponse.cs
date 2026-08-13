using CookieShop.App.DTOs.City;

namespace CookieShop.App.DTOs.Province;

public record ProvinceDetailResponse(
    Guid Id,
    string Name,
    ICollection<CityResponse> Cities
);