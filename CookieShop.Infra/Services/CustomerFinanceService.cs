using CookieShop.App.DTOs.Finance;
using CookieShop.App.Exceptions;
using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace CookieShop.Infra.Services;
public class CustomerFinanceService(CookieShopDbContext context)
{
    public async Task<AccountResponse> GetAccount(Guid userId, CancellationToken ct = default)
    {
        await EnsureCustomer(userId, ct);
        var balance = await context.FinancialEntries.Where(x => x.UserId == userId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var terms = await context.CustomerPaymentTerms.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var overdue = await (from charge in context.FinancialEntries
            join order in context.Orders on charge.OrderId equals order.Id
            where order.UserId == userId && order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Rejected
                && order.PaidAmount < order.TotalAmount && charge.Kind == "OrderCharge" && charge.DueAt <= DateTimeOffset.UtcNow
            select charge.Id).AnyAsync(ct);
        var reason = overdue ? "OVERDUE_DEBT: Please pay your overdue debt before placing another order."
            : terms?.DebtLimit is {} limit && -balance >= limit && balance < 0 ? "DEBT_LIMIT_REACHED: Please pay your debt before placing another order." : null;
        return new(balance, Math.Max(0, -balance), Math.Max(0, balance), terms?.DebtLimit, terms?.DueAt, reason, terms?.AllowPayLater == true);
    }
    public async Task<DateTimeOffset?> ChargeDueAt(Guid userId)
    {
        var terms = await context.CustomerPaymentTerms.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId);
        var due = terms?.DueAt;
        if (terms?.MonthlyDueDay is {} day)
        {
            var now = DateTimeOffset.UtcNow;
            var monthly = new DateTimeOffset(now.Year, now.Month, day, 0, 0, 0, TimeSpan.Zero);
            if (monthly <= now) monthly = monthly.AddMonths(1);
            if (due is null || monthly < due) due = monthly;
        }
        return due;
    }
    public async Task EnsureCanOrder(Guid userId, decimal additionalExposure, CancellationToken ct = default)
    {
        var account = await GetAccount(userId, ct);
        if (!account.AllowPayLater) throw new ConflictException("PAY_LATER_DISABLED: Pay later is not enabled for this customer.");
        if (account.OrderingBlockReason is {} reason) throw new ConflictException(reason);
        if (account.DebtLimit is {} limit && additionalExposure - account.Balance > limit)
            throw new ConflictException("DEBT_LIMIT_EXCEEDED: This order would exceed your debt limit. Please add funds or pay your debt.");
    }
    public async Task<IReadOnlyCollection<FinancialEntryResponse>> History(Guid userId, int page, CancellationToken ct)
    {
        await GetAccount(userId, ct);
        return await context.FinancialEntries.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((Math.Max(1, page) - 1) * 50).Take(50)
            .Select(x => new FinancialEntryResponse(x.Id, x.OrderId, x.Amount, x.Kind, x.Reference, x.RecordedBy, x.CreatedAt)).ToListAsync(ct);
    }
    public async Task<AccountResponse> SetTerms(Guid userId, PaymentTermsRequest request, CancellationToken ct)
    {
        if (request.MonthlyDueDay is < 1 or > 28) throw new ValidationException("Monthly due day must be between 1 and 28 (Gregorian UTC).");
        if (request.DebtLimit is {} limit && !StoreCurrency.IsValid(limit)) throw new ValidationException("Debt limit must be a supported whole-toman amount.");
        await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await GetAccount(userId, ct);
        var terms = await context.CustomerPaymentTerms.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (terms is null) { terms = new() { UserId = userId }; context.CustomerPaymentTerms.Add(terms); }
        terms.AllowPayLater = request.AllowPayLater;
        terms.MonthlyDueDay = request.MonthlyDueDay;
        terms.DebtLimit = request.DebtLimit;
        terms.DueAt = request.DueAt?.ToUniversalTime();
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAccount(userId, ct);
    }
    public async Task<AccountResponse> RecordPayment(Guid userId, Guid adminId, RecordPaymentRequest request, CancellationToken ct)
    {
        if (!StoreCurrency.IsValid(request.Amount) || request.Amount == 0 || string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > 128)
            throw new ValidationException("A positive whole-toman amount and a reference of at most 128 characters are required.");
        await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await GetAccount(userId, ct);
        var reference = request.Reference.Trim();
        var existing = await context.FinancialEntries.SingleOrDefaultAsync(x => x.UserId == userId && x.Reference == reference, ct);
        if (existing is not null)
        {
            if (existing.Kind != "Payment" || existing.Amount != request.Amount) throw new ConflictException("Payment reference already used for a different transaction.");
        }
        else
        {
            var entry = new FinancialEntry { UserId = userId, Amount = request.Amount, Kind = "Payment", Reference = reference, RecordedBy = adminId };
            context.FinancialEntries.Add(entry);
            await AllocateDebtPayment(userId, request.Amount, entry, ct);
            await context.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return await GetAccount(userId, ct);
    }
    public async Task EnsureCustomer(Guid userId, CancellationToken ct = default)
    {
        var roles = await (from membership in context.UserRoles
            join role in context.Roles on membership.RoleId equals role.Id
            where membership.UserId == userId select role.Name).ToListAsync(ct);
        if (!roles.Contains(UserRole.Customer) || roles.Contains(UserRole.Admin))
            throw new NotFoundException("Customer not found.");
    }

    // Caller owns the serializable transaction. Payments settle accepted debt oldest first.
    public async Task AllocateDebtPayment(Guid userId, decimal available, FinancialEntry source, CancellationToken ct = default)
    {
        var orders = await context.Orders.Where(x => x.UserId == userId &&
                (x.Status == OrderStatus.Accepted || x.Status == OrderStatus.Shipped || x.Status == OrderStatus.Delivered)
                && x.PaidAmount < x.TotalAmount)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
        foreach (var order in orders)
        {
            if (available <= 0) break;
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Rejected) continue;
            var amount = Math.Min(available, order.TotalAmount - order.PaidAmount);
            if (amount <= 0) continue;
            Allocate(order, amount, source, "DebtPayment");
            available -= amount;
        }
    }

    public void Allocate(Order order, decimal amount, FinancialEntry? source, string kind)
    {
        if (amount <= 0 || amount > order.TotalAmount - order.PaidAmount)
            throw new ConflictException("Invalid payment allocation.");
        context.OrderPayments.Add(new() { Order = order, Amount = amount, FinancialEntry = source, Source = kind });
        order.PaidAmount += amount;
        RefreshPaymentStatus(order);
        order.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static void RefreshPaymentStatus(Order order) => order.PaymentStatus =
        order.Status is OrderStatus.Cancelled or OrderStatus.Rejected && order.PaidAmount > 0 ? PaymentStatus.Refunded :
        order.PaidAmount >= order.TotalAmount ? PaymentStatus.Paid :
        order.PaidAmount > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Unpaid;

    public async Task PayFromWallet(Order order, CancellationToken ct = default)
    {
        var account = await GetAccount(order.UserId, ct);
        var amount = Math.Min(account.Credit, order.TotalAmount - order.PaidAmount);
        if (amount <= 0) return;
        // Accepted debt was charged already. Pending checkout is debited as it is funded.
        FinancialEntry? debit = null;
        if (order.Status == OrderStatus.Pending)
        {
            debit = new() { UserId = order.UserId, OrderId = order.Id, Amount = -amount, Kind = "OrderPrepayment" };
            context.FinancialEntries.Add(debit);
        }
        Allocate(order, amount, debit, "Wallet");
    }

}
