using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Auth;

public record RegisterRequest(
    [Required, RegularExpression(@"^09[0-9]{9}$")] string PhoneNumber,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Password
);