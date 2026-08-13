using System.ComponentModel.DataAnnotations;
using CookieShop.App.DTOs.User;

namespace CookieShop.App.DTOs.Address;

public record UpdateAddressRequest(
    [Required] Guid Id,
    [Required, MaxLength(64)] string Title,
    [Required, MaxLength(256)] string AddressLine1,
    [MaxLength(256)] string AddressLine2,
    [Required, MaxLength(16)] string PhoneNumber,
    [Required] Guid UserId,
    [Required] Guid ProvinceId,
    [Required] Guid CityId
);