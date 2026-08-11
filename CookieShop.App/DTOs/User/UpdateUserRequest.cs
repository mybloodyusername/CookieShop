using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.User;

public record UpdateUserRequest
{
    [Required] public required string Id { get; set; }
    [MaxLength(64)] public string FirstName { get; set; } = string.Empty;

    [MaxLength(64)] public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress] public required string Email { get; set; }

    [Required] public required string PhoneNumber { get; set; }
}