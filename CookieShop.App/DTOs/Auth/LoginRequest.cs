using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Auth;

public record LoginRequest
{
    [Required, RegularExpression(@"^09[0-9]{9}$")] public required string PhoneNumber { get; set; }
    [Required, MaxLength(128)] public required string Password { get; set; }
}