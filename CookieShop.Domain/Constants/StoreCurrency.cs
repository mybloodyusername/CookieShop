namespace CookieShop.Domain.Constants;

public static class StoreCurrency
{
    public const string Code = "IRT"; // Application identifier; not an ISO 4217 code.
    public const string Name = "تومان";
    public const decimal MaximumAmount = 999_999_999_999m;
    public static bool IsValid(decimal amount) => amount >= 0 && amount <= MaximumAmount && decimal.Truncate(amount) == amount;
    public static long ToRials(decimal toman)
    {
        if (!IsValid(toman)) throw new ArgumentOutOfRangeException(nameof(toman));
        return checked((long)toman * 10);
    }
}
