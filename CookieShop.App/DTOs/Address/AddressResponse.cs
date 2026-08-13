using CookieShop.App.DTOs.User;

namespace CookieShop.App.DTOs.Address;

public record AddressResponse(
    string Title,
    string AddressLine1,
    string AddressLine2,
    string PhoneNumber,
    string UserId,
    string ProvinceId,
    string CityId
);