using System.ComponentModel.DataAnnotations;
namespace CookieShop.App.DTOs.Finance;

/// <summary>Customer payment terms. Calendar deadlines are supplied as UTC instants.</summary>
/// <summary>Monthly-account terms. A zero limit disables billing, a positive limit caps it, and null is unlimited.</summary>
public sealed record PaymentTermsRequest(decimal? MonthlySpendingLimit = 0, DateTimeOffset? DueAt = null, int? MonthlyDueDay = null);
/// <summary>An explicit amount of a receipt allocated to one selected order.</summary>
public sealed record PaymentAllocationRequest(Guid OrderId, decimal Amount);
/// <summary>Money received outside the gateway. Unallocated funds remain wallet credit.</summary>
public sealed record RecordPaymentRequest(decimal Amount, [Required, MaxLength(128)] string Reference,
    [Required, MaxLength(32)] string Method, [Required, MaxLength(1024)] string Description,
    IReadOnlyCollection<PaymentAllocationRequest>? Allocations = null, DateTimeOffset? OccurredAt = null);
/// <summary>Money already returned externally to a customer, optionally against a paid order.</summary>
public sealed record RecordRefundRequest(decimal Amount, [Required, MaxLength(128)] string Reference,
    [Required, MaxLength(32)] string Method, [Required, MaxLength(1024)] string Description,
    Guid? OrderId = null, DateTimeOffset? OccurredAt = null);
/// <summary>An order allocation linked to a receipt in the financial history.</summary>
public sealed record PaymentAllocationResponse(Guid OrderId, string OrderNumber, decimal Amount);
/// <summary>An immutable financial posting, including receipt details and selected orders.</summary>
public sealed record FinancialEntryResponse(Guid Id, Guid? OrderId, decimal Amount, string Kind, string Reference,
    Guid? RecordedBy, DateTimeOffset CreatedAt, string Method, string Description, DateTimeOffset? OccurredAt,
    DateTimeOffset? DueAt, IReadOnlyCollection<PaymentAllocationResponse> Allocations);
/// <summary>Positive prepaid wallet funds and the separate monthly-account statement summary.</summary>
public sealed record AccountResponse(decimal WalletBalance, decimal MonthlyOutstanding, decimal? MonthlySpendingLimit,
    int? MonthlyDueDay, DateTimeOffset? NextDueAt, string? OrderingBlockReason, bool MonthlyBillingEnabled);
