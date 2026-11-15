using System.ComponentModel.DataAnnotations;
namespace CookieShop.App.DTOs.Finance;

/// <summary>Starts a gateway checkout for the outstanding order amount.</summary>
public sealed record StartPaymentRequest([Required, MaxLength(32)] string Provider);
/// <summary>Server-created gateway checkout and redirect destination.</summary>
public sealed record PaymentCheckoutResponse(Guid AttemptId, string RedirectUrl);
/// <summary>Payment allocation and fulfilment state of an order.</summary>
public sealed record OrderPaymentResponse(Guid OrderId, string OrderStatus, string PaymentStatus, decimal PaidAmount, decimal AmountDue);
