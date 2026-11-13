using System.ComponentModel.DataAnnotations;
namespace CookieShop.App.DTOs.Finance;
/// <summary>Customer payment terms. The client calculates calendar deadlines and sends their UTC instants.</summary>
public sealed record PaymentTermsRequest(decimal? DebtLimit, DateTimeOffset? DueAt, int? MonthlyDueDay = null);
/// <summary>A received payment or top-up. Reference must be unique for this customer.</summary>
public sealed record RecordPaymentRequest(decimal Amount, [Required, MaxLength(128)] string Reference);
/// <summary>A posting in the customer's financial history.</summary>
public sealed record FinancialEntryResponse(Guid Id, Guid? OrderId, decimal Amount, string Kind, string Reference, Guid? RecordedBy, DateTimeOffset CreatedAt);
/// <summary>Account balance: positive is credit, negative is debt.</summary>
public sealed record AccountResponse(decimal Balance, decimal Debt, decimal Credit, decimal? DebtLimit, DateTimeOffset? DueAt, string? OrderingBlockReason);
