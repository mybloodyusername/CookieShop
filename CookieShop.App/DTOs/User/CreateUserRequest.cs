using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.User;

public record CreateUserRequest
{
    [MaxLength(64)] public string FirstName { get; set; } = string.Empty;

    [MaxLength(64)] public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)] public required string Email { get; set; }

    [Required, RegularExpression(@"^09[0-9]{9}$")]
    public required string PhoneNumber { get; set; }

    [Required, StringLength(128, MinimumLength = 12)] public required string Password { get; set; }
};