using System.ComponentModel.DataAnnotations;
namespace CookieShop.App.DTOs.Finance;

/// <summary>Starts a gateway checkout for the outstanding order amount.</summary>
public sealed record StartPaymentRequest([Required, MaxLength(32)] string Provider);
/// <summary>Server-created gateway checkout and redirect destination.</summary>
public sealed record PaymentCheckoutResponse(Guid AttemptId, string RedirectUrl);
/// <summary>Payment allocation and fulfilment state of an order.</summary>
public sealed record OrderPaymentResponse(Guid OrderId, string OrderStatus, string PaymentStatus, decimal PaidAmount, decimal AmountDue, decimal RefundedAmount);

/// <summary>Funds a wallet before order creation through a configured, server-verified gateway.</summary>
public sealed record StartWalletTopUpRequest(decimal Amount, [Required, MaxLength(32)] string Provider);
