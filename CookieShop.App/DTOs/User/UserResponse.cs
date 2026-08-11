namespace CookieShop.App.DTOs.User;

public record UserResponse
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
};