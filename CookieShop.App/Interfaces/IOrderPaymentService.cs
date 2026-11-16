using CookieShop.App.DTOs.Finance;
namespace CookieShop.App.Interfaces;
public interface IOrderPaymentService
{
    Task<OrderPaymentResponse> PayWithWallet(Guid userId, Guid orderId, CancellationToken ct);
    Task<PaymentCheckoutResponse> Start(Guid userId, Guid orderId, string provider, string apiBaseUrl, CancellationToken ct);
    Task<PaymentCheckoutResponse> StartWalletTopUp(Guid userId, StartWalletTopUpRequest request, string apiBaseUrl, CancellationToken ct);
    Task ChooseMockOutcome(Guid attemptId, bool succeeded, CancellationToken ct);
    Task Verify(Guid attemptId, CancellationToken ct);
}
