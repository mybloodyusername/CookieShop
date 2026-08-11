using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Auth;

public record LoginRequest
{
    [Required] public required string PhoneNumber { get; set; }
    [Required] public required string Password { get; set; }
}