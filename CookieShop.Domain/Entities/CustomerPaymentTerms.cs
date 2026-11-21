namespace CookieShop.Domain.Entities;
public class CustomerPaymentTerms
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int? MonthlyDueDay { get; set; }
    /// <summary>Zero disables monthly billing; null is unlimited; positive values cap monthly exposure.</summary>
    public decimal? MonthlySpendingLimit { get; set; } = 0;
}
