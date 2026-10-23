using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.User;

public record UpdateUserRequest
{
    [Required] public required Guid Id { get; set; }
    [MaxLength(64)] public string FirstName { get; set; } = string.Empty;

    [MaxLength(64)] public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)] public required string Email { get; set; }

    [Required, RegularExpression(@"^09[0-9]{9}$")] public required string PhoneNumber { get; set; }
}