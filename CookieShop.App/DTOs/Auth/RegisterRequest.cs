using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Auth;

public record RegisterRequest(
    [Required] string PhoneNumber,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);