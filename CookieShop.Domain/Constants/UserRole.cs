namespace CookieShop.Domain.Constants;

public static class UserRole
{
    public const string Admin = nameof(Admin);
    public const string Customer = nameof(Customer);

    public static readonly string[] All = [Admin, Customer];
}