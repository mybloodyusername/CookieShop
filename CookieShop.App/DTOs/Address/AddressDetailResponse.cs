using CookieShop.App.DTOs.User;

namespace CookieShop.App.DTOs.Address;

public record AddressDetailResponse(
    Guid Id,
    string Title,
    string AddressLine1,
    string AddressLine2,
    string PhoneNumber,
    Guid UserId,
    UserResponse User,
    Guid ProvinceId,
    Guid CityId
);