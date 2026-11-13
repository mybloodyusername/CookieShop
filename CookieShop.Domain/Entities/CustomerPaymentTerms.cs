namespace CookieShop.Domain.Entities;
public class CustomerPaymentTerms
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int? MonthlyDueDay { get; set; }
    public decimal? DebtLimit { get; set; }
    public DateTimeOffset? DueAt { get; set; }
}
