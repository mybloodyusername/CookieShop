namespace CookieShop.Domain.Entities;

/// <summary>An immutable financial posting: positive amounts are customer credit, negative amounts are charges.</summary>
public class FinancialEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Kind { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Method { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset? OccurredAt { get; set; }
    public Guid? RecordedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
