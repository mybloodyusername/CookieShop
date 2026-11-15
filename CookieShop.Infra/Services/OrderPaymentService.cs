using System.Data;
using CookieShop.App.DTOs.Finance;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
namespace CookieShop.Infra.Services;

public sealed class OrderPaymentService(CookieShopDbContext context, CustomerFinanceService finance,
    IEnumerable<IPaymentGateway> gateways) : IOrderPaymentService
{
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

    public async Task<PaymentCheckoutResponse> Start(Guid userId, Guid orderId, string provider, CancellationToken ct)
    {
        await finance.EnsureCustomer(userId, ct);
        var order = await OwnedOrder(userId, orderId, ct);
        EnsurePayable(order);
        var gateway = Gateway(provider);
        var attempt = new PaymentAttempt { OrderId = orderId, Amount = order.TotalAmount - order.PaidAmount, Provider = provider };
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
        if (snapshot.Status == "Creating") throw new ConflictException("Gateway checkout has not completed initialization.");
        var verified = await Gateway(snapshot.Provider).Verify(snapshot.Authority, ct);
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
            var order = await context.Orders.SingleAsync(x => x.Id == attempt.OrderId, ct);
            await finance.EnsureCustomer(order.UserId, ct);
            var receipt = new FinancialEntry { UserId = order.UserId, OrderId = order.Id, Amount = attempt.Amount, Kind = "GatewayPayment",
                Reference = $"gateway:{attempt.Id:N}" };
            context.FinancialEntries.Add(receipt);
            var amount = Math.Min(attempt.Amount, order.TotalAmount - order.PaidAmount);
            if (amount > 0)
            {
                if (order.Status == OrderStatus.Pending)
                    context.FinancialEntries.Add(new() { UserId = order.UserId, OrderId = order.Id, Amount = -amount, Kind = "OrderPrepayment" });
                // Cancelled/rejected orders retain the receipt as wallet credit, without a new debit.
                finance.Allocate(order, amount, receipt, "Gateway");
            }
            // Excess receipts (e.g. two successful attempts) remain wallet credit or settle other debt.
            var remaining = attempt.Amount - (order.Status is OrderStatus.Cancelled or OrderStatus.Rejected ? 0 : amount);
            if (remaining > 0) await finance.AllocateDebtPayment(order.UserId, remaining, receipt, ct);
            attempt.Status = "Succeeded";
            attempt.TransactionId = verified.TransactionId;
        }
        attempt.CompletedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
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
        order.PaymentStatus.ToString(), order.PaidAmount, Math.Max(0, order.TotalAmount - order.PaidAmount));
}
