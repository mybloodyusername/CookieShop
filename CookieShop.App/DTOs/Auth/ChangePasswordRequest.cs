using System.ComponentModel.DataAnnotations;
namespace CookieShop.App.DTOs.Auth;
public record ChangePasswordRequest(
    [Required, MaxLength(128)] string CurrentPassword,
    [Required, StringLength(128, MinimumLength = 12)] string NewPassword);
