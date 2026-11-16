using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace CookieShop.App.DTOs.Finance;

/// <summary>Starts a gateway checkout for the outstanding order amount.</summary>
public sealed record StartPaymentRequest([Required, MaxLength(32)] string Provider);
/// <summary>Server-created gateway checkout and redirect destination.</summary>
public sealed record PaymentCheckoutResponse(Guid? AttemptId, string? RedirectUrl, MockZarinPalResponse? MockResponse = null);
/// <summary>Development-only mock of ZarinPal payment-request acknowledgement. It is not a payment confirmation.</summary>
public sealed record MockZarinPalResponse(
    [property: JsonPropertyName("data")] MockZarinPalData Data,
    [property: JsonPropertyName("errors")] IReadOnlyCollection<object> Errors);
public sealed record MockZarinPalData(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("authority")] string Authority,
    [property: JsonPropertyName("fee_type")] string FeeType,
    [property: JsonPropertyName("fee")] decimal Fee);
/// <summary>Payment allocation and fulfilment state of an order.</summary>
public sealed record OrderPaymentResponse(Guid OrderId, string OrderStatus, string PaymentStatus, decimal PaidAmount, decimal AmountDue, decimal RefundedAmount);

/// <summary>Funds a wallet before order creation through a configured, server-verified gateway.</summary>
public sealed record StartWalletTopUpRequest(decimal Amount, [Required, MaxLength(32)] string Provider);
