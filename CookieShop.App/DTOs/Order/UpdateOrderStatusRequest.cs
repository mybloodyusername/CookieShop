using System.ComponentModel.DataAnnotations;

namespace CookieShop.App.DTOs.Order;

public sealed record UpdateOrderStatusRequest(
    [Required] Guid OrderId,
    [Required] string Status,
    [MaxLength(512)] string? Reason = null
);

/// <summary>Why the customer is withdrawing their pending order.</summary>
public sealed record CancelOrderRequest([Required, MaxLength(512)] string Reason);
/// <summary>An auditable order transition, including who performed it and why.</summary>
public sealed record OrderStatusChangeResponse(string? FromStatus, string ToStatus, Guid? ActorId, string Reason, DateTimeOffset CreatedAt);
