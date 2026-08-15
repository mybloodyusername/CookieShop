namespace CookieShop.App.DTOs.User;

public record UserResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public string? Role { get; set; }
};