namespace CookieShop.App.Interfaces;

/// <summary>Implement using the provider SDK/API. Amounts here are whole IRT; convert at the adapter boundary.</summary>
public interface IPaymentGateway
{
    string Provider { get; }
    // Use attemptId as the merchant idempotency key and configure callback URLs on the server.
    Task<GatewayCheckout> Create(Guid attemptId, decimal amount, CancellationToken ct);
    // Verify against the provider server using the stored authority; never trust browser callback claims.
    Task<GatewayVerification> Verify(string authority, CancellationToken ct);
}
public sealed record GatewayCheckout(string Authority, string RedirectUrl);
public sealed record GatewayVerification(bool Succeeded, decimal Amount, string? TransactionId);
