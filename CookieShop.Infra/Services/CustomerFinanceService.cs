using CookieShop.App.DTOs.Finance;
using CookieShop.App.Exceptions;
using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
namespace CookieShop.Infra.Services;
public class CustomerFinanceService(CookieShopDbContext context)
{
    public async Task<AccountResponse> GetAccount(Guid userId, CancellationToken ct = default)
    {
        await EnsureCustomer(userId, ct);
        var ledgerTotal = await context.FinancialEntries.Where(x => x.UserId == userId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var monthlyOutstanding = await context.Orders.Where(x => x.UserId == userId && x.MonthlyBilling &&
            (x.Status == OrderStatus.Accepted || x.Status == OrderStatus.Shipped || x.Status == OrderStatus.Delivered))
            .SumAsync(x => (decimal?)(x.TotalAmount - x.PaidAmount), ct) ?? 0;
        var walletBalance = Math.Max(0, ledgerTotal + monthlyOutstanding);
        var terms = await context.CustomerPaymentTerms.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var monthlyBillingEnabled = terms is not null && (terms.MonthlySpendingLimit is null || terms.MonthlySpendingLimit > 0);
        var nextDueAt = await (from charge in context.FinancialEntries
            join order in context.Orders on charge.OrderId equals order.Id
            where order.UserId == userId && order.MonthlyBilling && order.PaidAmount < order.TotalAmount
                && order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Rejected
                && charge.Kind == "MonthlyCharge"
            select charge.DueAt).MinAsync(ct);
        var overdue = await (from charge in context.FinancialEntries
            join order in context.Orders on charge.OrderId equals order.Id
            where order.UserId == userId && order.MonthlyBilling && order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Rejected
                && order.PaidAmount < order.TotalAmount && charge.Kind == "MonthlyCharge" && charge.DueAt <= DateTimeOffset.UtcNow
            select charge.Id).AnyAsync(ct);
        var limit = terms?.MonthlySpendingLimit;
        var netMonthlyExposure = Math.Max(0, monthlyOutstanding - walletBalance);
        var reason = overdue ? "OVERDUE_MONTHLY_ACCOUNT: Please settle your overdue monthly account before placing another order."
            : monthlyBillingEnabled && limit is {} spendingLimit && netMonthlyExposure >= spendingLimit && monthlyOutstanding > 0
                ? "MONTHLY_SPENDING_LIMIT_REACHED: Please settle your monthly account before placing another order."
                : null;
        return new(walletBalance, monthlyOutstanding, limit, terms?.MonthlyDueDay, nextDueAt, reason, monthlyBillingEnabled);
    }
    public async Task<DateTimeOffset?> ChargeDueAt(Guid userId)
    {
        var terms = await context.CustomerPaymentTerms.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId);
        if (terms?.MonthlyDueDay is not { } day) return null;
        var calendar = new PersianCalendar();
        var now = DateTime.UtcNow;
        var year = calendar.GetYear(now);
        var month = calendar.GetMonth(now);
        var dayThisMonth = Math.Min(day, calendar.GetDaysInMonth(year, month));
        var dueDate = calendar.ToDateTime(year, month, dayThisMonth, 0, 0, 0, 0);
        if (dueDate <= now)
        {
            month++;
            if (month > calendar.GetMonthsInYear(year)) { month = 1; year++; }
            var dayNextMonth = Math.Min(day, calendar.GetDaysInMonth(year, month));
            dueDate = calendar.ToDateTime(year, month, dayNextMonth, 0, 0, 0, 0);
        }
        return new DateTimeOffset(DateTime.SpecifyKind(dueDate, DateTimeKind.Utc));
    }
    public async Task EnsureCanOrder(Guid userId, decimal additionalExposure, CancellationToken ct = default)
    {
        var account = await GetAccount(userId, ct);
        if (!account.MonthlyBillingEnabled) throw new ConflictException("MONTHLY_BILLING_DISABLED: Monthly billing is not enabled for this customer.");
        if (account.OrderingBlockReason is {} reason) throw new ConflictException(reason);
        if (account.MonthlySpendingLimit is {} limit && additionalExposure + account.MonthlyOutstanding - account.WalletBalance > limit)
            throw new ConflictException("MONTHLY_SPENDING_LIMIT_EXCEEDED: This order would exceed the customer's monthly spending limit.");
    }
    public async Task<IReadOnlyCollection<FinancialEntryResponse>> History(Guid userId, int page, CancellationToken ct)
    {
        await EnsureCustomer(userId, ct);
        var entries = await context.FinancialEntries.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((Math.Max(1, page) - 1) * 50).Take(50).ToListAsync(ct);
        return await MapEntries(entries, ct);
    }

    public async Task<FinancialEntryResponse> GetEntry(Guid userId, Guid entryId, CancellationToken ct)
    {
        await EnsureCustomer(userId, ct);
        var entry = await context.FinancialEntries.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Id == entryId, ct)
            ?? throw new NotFoundException("Financial entry not found.");
        return (await MapEntries([entry], ct)).Single();
    }

    private async Task<IReadOnlyCollection<FinancialEntryResponse>> MapEntries(IReadOnlyCollection<FinancialEntry> entries, CancellationToken ct)
    {
        var ids = entries.Select(x => x.Id).ToList();
        var allocations = await context.OrderPayments.AsNoTracking().Where(x => x.FinancialEntryId != null && ids.Contains(x.FinancialEntryId.Value))
            .Select(x => new { EntryId = x.FinancialEntryId!.Value, Allocation = new PaymentAllocationResponse(x.OrderId, x.Order.OrderNumber, x.Amount) })
            .ToListAsync(ct);
        return entries.Select(x => new FinancialEntryResponse(x.Id, x.OrderId, x.Amount, x.Kind, x.Reference, x.RecordedBy,
            x.CreatedAt, x.Method, x.Description, x.OccurredAt, x.DueAt,
            allocations.Where(a => a.EntryId == x.Id).Select(a => a.Allocation).ToList())).ToList();
    }

    public async Task<AccountResponse> SetTerms(Guid userId, PaymentTermsRequest request, CancellationToken ct)
    {
        if (request.MonthlyDueDay is < 1 or > 31) throw new ValidationException("Monthly due day must be between 1 and 31.");
        if (request.MonthlySpendingLimit is {} limit && (!StoreCurrency.IsValid(limit) || limit < 0))
            throw new ValidationException("Monthly spending limit must be zero, unlimited, or a supported positive whole-toman amount.");
        await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await GetAccount(userId, ct);
        var terms = await context.CustomerPaymentTerms.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (terms is null) { terms = new() { UserId = userId }; context.CustomerPaymentTerms.Add(terms); }
        terms.MonthlyDueDay = request.MonthlyDueDay;
        terms.MonthlySpendingLimit = request.MonthlySpendingLimit;
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await GetAccount(userId, ct);
    }
    private static void ValidateReceipt(decimal amount, string? reference, string method, string description, DateTimeOffset? occurredAt, bool requireReference = true)
    {
        if (!StoreCurrency.IsValid(amount) || amount == 0 || (requireReference && string.IsNullOrWhiteSpace(reference)) || (reference?.Length ?? 0) > 128 ||
            string.IsNullOrWhiteSpace(method) || method.Length > 32 || string.IsNullOrWhiteSpace(description) || description.Length > 1024)
            throw new ValidationException(requireReference
                ? "A positive whole-toman amount, reference (128), method (32), and description (1024) are required."
                : "A positive whole-toman amount, method (32), and description (1024) are required; an optional reference cannot exceed 128 characters.");
        if (occurredAt > DateTimeOffset.UtcNow.AddMinutes(1))
            throw new ValidationException("Payment/refund occurrence cannot be in the future.");
    }

    public async Task<FinancialEntryResponse> RecordPayment(Guid userId, Guid adminId, RecordPaymentRequest request, CancellationToken ct)
    {
        if (request.Method is not ("PAYA" or "CARD")) throw new ValidationException("Payment method must be PAYA or CARD.");
        ValidateReceipt(request.Amount, request.Reference, request.Method, request.Description, request.OccurredAt, requireReference: false);
        var allocations = request.Allocations?.ToList() ?? [];
        if (allocations.Count > 500 || allocations.Any(a => a.OrderId == Guid.Empty || !StoreCurrency.IsValid(a.Amount) || a.Amount == 0) ||
            allocations.Select(a => a.OrderId).Distinct().Count() != allocations.Count)
            throw new ValidationException("Select distinct orders with positive whole-toman allocations (maximum 500 orders).");
        await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await EnsureCustomer(userId, ct);
        var reference = request.Reference?.Trim() ?? string.Empty;
        FinancialEntry? existing = null;
        if (reference.Length > 0)
            existing = await context.FinancialEntries.SingleOrDefaultAsync(x => x.UserId == userId && x.Reference == reference, ct);
        if (existing is not null)
        {
            var saved = await context.OrderPayments.Where(x => x.FinancialEntryId == existing.Id).ToListAsync(ct);
            if (!Matches(existing, "Payment", request.Amount, request.Method, request.Description, request.OccurredAt) ||
                saved.Count != allocations.Count || saved.Any(p => !allocations.Any(a => a.OrderId == p.OrderId && a.Amount == p.Amount)))
                throw new ConflictException("Payment reference already used for a different transaction.");
        }
        else
        {
            var account = await GetAccount(userId, ct);
            if (allocations.Sum(a => a.Amount) > request.Amount + account.WalletBalance)
                throw new ConflictException("Selected orders exceed the receipt amount and available wallet balance.");
            var ids = allocations.Select(a => a.OrderId).ToList();
            var orders = await context.Orders.Where(o => ids.Contains(o.Id) && o.UserId == userId).ToDictionaryAsync(o => o.Id, ct);
            if (orders.Count != ids.Count) throw new NotFoundException("A selected order does not belong to this customer.");
            foreach (var allocation in allocations)
            {
                var order = orders[allocation.OrderId];
                if (order.Status is OrderStatus.Cancelled or OrderStatus.Rejected || allocation.Amount > order.TotalAmount - order.PaidAmount)
                    throw new ConflictException("An allocation exceeds an order's outstanding amount or targets a closed order.");
            }
            existing = new FinancialEntry { UserId = userId, Amount = request.Amount, Kind = "Payment", Reference = reference,
                Method = request.Method.Trim(), Description = request.Description.Trim(), OccurredAt = request.OccurredAt?.ToUniversalTime(), RecordedBy = adminId };
            context.FinancialEntries.Add(existing);
            foreach (var allocation in allocations)
            {
                var order = orders[allocation.OrderId];
                if (order.Status == OrderStatus.Pending)
                    context.FinancialEntries.Add(new() { UserId = userId, OrderId = order.Id, Amount = -allocation.Amount, Kind = "OrderPrepayment" });
                else if (!order.MonthlyBilling)
                    context.FinancialEntries.Add(new() { UserId = userId, OrderId = order.Id, Amount = -allocation.Amount, Kind = "WalletDebit" });
                Allocate(order, allocation.Amount, existing, "Manual");
            }
            await context.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return await GetEntry(userId, existing.Id, ct);
    }

    private static bool Matches(FinancialEntry entry, string kind, decimal amount, string method, string description, DateTimeOffset? occurredAt) =>
        entry.Kind == kind && entry.Amount == amount && entry.Method == method.Trim() && entry.Description == description.Trim() &&
        entry.OccurredAt == occurredAt?.ToUniversalTime();

    public async Task<FinancialEntryResponse> RecordRefund(Guid userId, Guid adminId, RecordRefundRequest request, CancellationToken ct)
    {
        ValidateReceipt(request.Amount, request.Reference, request.Method, request.Description, request.OccurredAt);
        await using var tx = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var account = await GetAccount(userId, ct);
        var reference = request.Reference.Trim();
        var entry = await context.FinancialEntries.SingleOrDefaultAsync(x => x.UserId == userId && x.Reference == reference, ct);
        if (entry is not null)
        {
            if (!Matches(entry, "Refund", -request.Amount, request.Method, request.Description, request.OccurredAt) || entry.OrderId != request.OrderId)
                throw new ConflictException("Refund reference already used for a different transaction.");
        }
        else
        {
            Order? order = null;
            if (request.OrderId is {} orderId)
            {
                order = await context.Orders.SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId, ct)
                    ?? throw new NotFoundException("Order not found for this customer.");
                if (request.Amount > order.PaidAmount - order.RefundedAmount)
                    throw new ConflictException("Refund exceeds the order's remaining refundable paid amount.");
            }
            if (order is null || order.Status is OrderStatus.Cancelled or OrderStatus.Rejected)
            {
                if (request.Amount > account.WalletBalance)
                    throw new ConflictException("Refund exceeds available wallet funds. Released funds may already have been spent.");
            }
            else
            {
                // A live-order refund is a price credit plus a cash payout. It does
                // not reopen paid debt or change fulfilment, even after delivery.
                context.FinancialEntries.Add(new() { UserId = userId, OrderId = order.Id, Amount = request.Amount,
                    Kind = "OrderRefundCredit", RecordedBy = adminId, Description = request.Description.Trim() });
            }
            entry = new() { UserId = userId, OrderId = order?.Id, Amount = -request.Amount, Kind = "Refund", Reference = reference,
                Method = request.Method.Trim(), Description = request.Description.Trim(), OccurredAt = request.OccurredAt?.ToUniversalTime(), RecordedBy = adminId };
            context.FinancialEntries.Add(entry);
            if (order is not null)
            {
                order.RefundedAmount += request.Amount;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                RefreshPaymentStatus(order);
            }
            await context.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return await GetEntry(userId, entry.Id, ct);
    }

    public async Task EnsureCustomer(Guid userId, CancellationToken ct = default)
    {
        var roles = await (from membership in context.UserRoles
            join role in context.Roles on membership.RoleId equals role.Id
            where membership.UserId == userId select role.Name).ToListAsync(ct);
        if (!roles.Contains(UserRole.Customer) || roles.Contains(UserRole.Admin))
            throw new NotFoundException("Customer not found.");
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

    public static void RefreshPaymentStatus(Order order)
    {
        var closed = order.Status is OrderStatus.Cancelled or OrderStatus.Rejected;
        order.PaymentStatus = order.RefundedAmount > 0
            ? order.RefundedAmount == order.PaidAmount && (closed || order.PaidAmount == order.TotalAmount)
                ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded
            : closed && order.PaidAmount > 0 ? PaymentStatus.WalletCredited
            : order.PaidAmount >= order.TotalAmount ? PaymentStatus.Paid
            : order.PaidAmount > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Unpaid;
    }

    public async Task PayFromWallet(Order order, CancellationToken ct = default)
    {
        var account = await GetAccount(order.UserId, ct);
        var amount = Math.Min(account.WalletBalance, order.TotalAmount - order.PaidAmount);
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
