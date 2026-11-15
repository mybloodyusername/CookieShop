using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace CookieShop.App.DTOs.User;

/// <summary>Customer-editable profile fields. Email and phone changes require an administrator.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateMyProfileRequest(
    [property: MaxLength(64)] string FirstName,
    [property: MaxLength(64)] string LastName);
