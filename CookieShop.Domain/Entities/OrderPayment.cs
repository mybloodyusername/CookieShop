namespace CookieShop.Domain.Entities;

/// <summary>Immutable allocation of received money to an order, independent of its fulfilment state.</summary>
public class OrderPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid? FinancialEntryId { get; set; }
    public FinancialEntry? FinancialEntry { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
