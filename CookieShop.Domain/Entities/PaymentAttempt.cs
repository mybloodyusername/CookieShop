namespace CookieShop.Domain.Entities;

/// <summary>A gateway attempt. Only a server-verified success may post money.</summary>
public class PaymentAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Provider { get; set; } = "";
    public string Authority { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? TransactionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
