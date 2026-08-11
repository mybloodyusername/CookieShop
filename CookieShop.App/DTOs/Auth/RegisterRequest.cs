using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Auth;

public record RegisterRequest
{
    [Required] public required string PhoneNumber { get; set; }
    [Required, EmailAddress] public required string Email { get; set; }
    [Required, MinLength(8)] public required string Password { get; set; }
};