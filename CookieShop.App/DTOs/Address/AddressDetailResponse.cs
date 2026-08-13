using CookieShop.App.DTOs.City;
using CookieShop.App.DTOs.Province;
using CookieShop.App.DTOs.User;

namespace CookieShop.App.DTOs.Address;

public record AddressDetailResponse(
    string Title,
    string AddressLine1,
    string AddressLine2,
    string PhoneNumber,
    string UserId,
    UserResponse User,
    string ProvinceId,
    ProvinceResponse Province,
    string CityId,
    CityResponse City
);