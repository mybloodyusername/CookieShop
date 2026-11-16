using System.Data;
using System.ComponentModel.DataAnnotations;
using CookieShop.Domain.Constants;
using CookieShop.App.DTOs.Finance;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace CookieShop.Infra.Services;

public sealed class OrderPaymentService(
    CookieShopDbContext context,
    CustomerFinanceService finance,
    IEnumerable<IPaymentGateway> gateways,
    IHostEnvironment environment) : IOrderPaymentService
{
    // TODO: gateway implementation — register the real ZarinPal adapter here when credentials and callback URLs are ready.
    private IPaymentGateway Gateway(string provider) => gateways.SingleOrDefault(x => x.Provider == provider)
        ?? throw new ConflictException("This payment gateway is not configured.");

    public async Task<OrderPaymentResponse> PayWithWallet(Guid userId, Guid orderId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await finance.EnsureCustomer(userId, ct);
        var order = await OwnedOrder(userId, orderId, ct);
        EnsurePayable(order);
        await finance.PayFromWallet(order, ct);
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Response(order);
    }

    public async Task<PaymentCheckoutResponse> Start(Guid userId, Guid orderId, string provider, string apiBaseUrl, CancellationToken ct)
    {
        await finance.EnsureCustomer(userId, ct);
        var order = await OwnedOrder(userId, orderId, ct);
        EnsurePayable(order);
        var attempt = new PaymentAttempt
        {
            UserId = userId,
            OrderId = orderId,
            Amount = order.TotalAmount - order.PaidAmount,
            Provider = provider
        };
        return IsDevelopmentMock(provider)
            ? await CreateMockAttempt(attempt, apiBaseUrl, ct)
            : await CreateAttempt(attempt, ct);
    }

    public async Task<PaymentCheckoutResponse> StartWalletTopUp(Guid userId, StartWalletTopUpRequest request, string apiBaseUrl, CancellationToken ct)
    {
        if (!StoreCurrency.IsValid(request.Amount) || request.Amount == 0)
            throw new ValidationException("Top-up must be a positive whole-toman amount.");
        await finance.EnsureCustomer(userId, ct);
        var attempt = new PaymentAttempt { UserId = userId, Amount = request.Amount, Provider = request.Provider };
        return IsDevelopmentMock(request.Provider)
            ? await CreateMockAttempt(attempt, apiBaseUrl, ct)
            : await CreateAttempt(attempt, ct);
    }

    public async Task ChooseMockOutcome(Guid attemptId, bool succeeded, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) throw new NotFoundException("Mock checkout not found.");
        var attempt = await context.PaymentAttempts.SingleOrDefaultAsync(x => x.Id == attemptId, ct)
            ?? throw new NotFoundException("Payment attempt not found.");
        if (attempt.Provider != "zarinpal-mock") throw new NotFoundException("Mock checkout not found.");

        var selected = succeeded ? "MockSucceeded" : "MockRejected";
        if (attempt.Status == selected) return;
        if (attempt.Status != "Pending") throw new ConflictException("A mock payment outcome was already selected.");
        attempt.Status = selected;
        await context.SaveChangesAsync(ct);
    }

    private bool IsDevelopmentMock(string provider) =>
        environment.IsDevelopment() && provider == "zarinpal-mock";

    private async Task<PaymentCheckoutResponse> CreateMockAttempt(PaymentAttempt attempt, string apiBaseUrl, CancellationToken ct)
    {
        if (!environment.IsDevelopment() ||
            !Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
            throw new ConflictException("Development mock checkout is unavailable.");

        attempt.Status = "Pending";
        attempt.Authority = $"mock:{attempt.Id:N}";
        context.PaymentAttempts.Add(attempt);
        await context.SaveChangesAsync(ct);
        var redirectUrl = new Uri(baseUri, $"/api/payments/mock-checkout/{attempt.Id:D}").ToString();
        return new(attempt.Id, redirectUrl);
    }

    private async Task<PaymentCheckoutResponse> CreateAttempt(PaymentAttempt attempt, CancellationToken ct)
    {
        var gateway = Gateway(attempt.Provider);
        // Persist the merchant key before contacting the provider so interrupted checkouts are auditable.
        attempt.Status = "Creating";
        attempt.Authority = $"creating:{attempt.Id:N}";
        context.PaymentAttempts.Add(attempt);
        await context.SaveChangesAsync(ct);
        var checkout = await gateway.Create(attempt.Id, attempt.Amount, ct);
        if (string.IsNullOrWhiteSpace(checkout.Authority) || checkout.Authority.Length > 128 ||
            !Uri.TryCreate(checkout.RedirectUrl, UriKind.Absolute, out var redirect) || redirect.Scheme != "https")
            throw new ConflictException("The payment gateway returned an invalid checkout.");
        attempt.Authority = checkout.Authority;
        attempt.Status = "Pending";
        await context.SaveChangesAsync(ct);
        return new(attempt.Id, checkout.RedirectUrl);
    }

    public async Task Verify(Guid attemptId, CancellationToken ct)
    {
        var snapshot = await context.PaymentAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attemptId, ct)
            ?? throw new NotFoundException("Payment attempt not found.");
        if (snapshot.Status == "Succeeded") return;
        if (snapshot.Status == "Failed") throw new ConflictException("The payment was rejected.");
        if (snapshot.Status == "Creating") throw new ConflictException("Gateway checkout has not completed initialization.");

        GatewayVerification verified;
        if (IsDevelopmentMock(snapshot.Provider))
        {
            verified = snapshot.Status switch
            {
                "MockSucceeded" => new(true, snapshot.Amount, $"mock:{snapshot.Id:N}"),
                "MockRejected" => new(false, snapshot.Amount, null),
                _ => throw new ConflictException("Choose success or rejection on the mock checkout page first.")
            };
        }
        else
        {
            verified = await Gateway(snapshot.Provider).Verify(snapshot.Authority, ct);
        }

        if (verified.Succeeded && (verified.Amount != snapshot.Amount || string.IsNullOrWhiteSpace(verified.TransactionId) || verified.TransactionId.Length > 128))
            throw new ConflictException("Gateway verification did not match the expected payment.");

        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var attempt = await context.PaymentAttempts.SingleAsync(x => x.Id == attemptId, ct);
        if (attempt.Status == "Succeeded") return;
        if (!verified.Succeeded)
        {
            attempt.Status = "Failed";
        }
        else
        {
            await finance.EnsureCustomer(attempt.UserId, ct);
            var order = attempt.OrderId is { } orderId
                ? await context.Orders.SingleAsync(x => x.Id == orderId && x.UserId == attempt.UserId, ct)
                : null;
            var receipt = new FinancialEntry
            {
                UserId = attempt.UserId,
                OrderId = order?.Id,
                Amount = attempt.Amount,
                Kind = "GatewayPayment",
                Reference = $"gateway:{attempt.Id:N}",
                Method = "Gateway",
                Description = $"{attempt.Provider}: {verified.TransactionId}",
                OccurredAt = DateTimeOffset.UtcNow
            };
            context.FinancialEntries.Add(receipt);
            if (order is not null && order.Status is not (OrderStatus.Cancelled or OrderStatus.Rejected))
            {
                var amount = Math.Min(attempt.Amount, order.TotalAmount - order.PaidAmount);
                if (amount > 0)
                {
                    if (order.Status == OrderStatus.Pending)
                        context.FinancialEntries.Add(new() { UserId = order.UserId, OrderId = order.Id, Amount = -amount, Kind = "OrderPrepayment" });
                    finance.Allocate(order, amount, receipt, "Gateway");
                }
            }
            // Top-ups, excess and late payments remain wallet credit. Other orders
            // are never silently marked paid.
            attempt.Status = "Succeeded";
            attempt.TransactionId = verified.TransactionId;
        }
        attempt.CompletedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        if (!verified.Succeeded) throw new ConflictException("The payment was rejected.");
    }

    private async Task<Order> OwnedOrder(Guid userId, Guid orderId, CancellationToken ct) =>
        await context.Orders.SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId, ct)
        ?? throw new NotFoundException("Order not found.");

    private static void EnsurePayable(Order order)
    {
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Rejected || order.PaidAmount >= order.TotalAmount)
            throw new ConflictException("This order has no payable amount.");
    }

    private static OrderPaymentResponse Response(Order order) => new(order.Id, order.Status.ToString(),
        order.PaymentStatus.ToString(), order.PaidAmount, Math.Max(0, order.TotalAmount - order.PaidAmount), order.RefundedAmount);
}
