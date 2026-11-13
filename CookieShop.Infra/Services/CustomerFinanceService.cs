using CookieShop.App.DTOs.Finance;
using CookieShop.App.Exceptions;
using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace CookieShop.Infra.Services;
public class CustomerFinanceService(CookieShopDbContext context)
{
    public async Task<AccountResponse> GetAccount(Guid userId, CancellationToken ct = default)
    {
        if (!await context.Users.AnyAsync(x => x.Id == userId, ct)) throw new NotFoundException("Customer not found.");
        var balance = await context.FinancialEntries.Where(x => x.UserId == userId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var terms = await context.CustomerPaymentTerms.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var entries = await context.FinancialEntries.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
        var credits = entries.Where(x => x.Amount > 0).Sum(x => x.Amount);
        var overdue = false;
        foreach (var charge in entries.Where(x => x.Amount < 0))
        {
            var applied = Math.Min(credits, -charge.Amount);
            credits -= applied;
            if (-charge.Amount > applied && charge.DueAt <= DateTimeOffset.UtcNow) overdue = true;
        }
        var reason = overdue ? "OVERDUE_DEBT: Please pay your overdue debt before placing another order."
            : terms?.DebtLimit is {} limit && -balance >= limit && balance < 0 ? "DEBT_LIMIT_REACHED: Please pay your debt before placing another order." : null;
        return new(balance, Math.Max(0, -balance), Math.Max(0, balance), terms?.DebtLimit, terms?.DueAt, reason);
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
            context.FinancialEntries.Add(new() { UserId = userId, Amount = request.Amount, Kind = "Payment", Reference = reference, RecordedBy = adminId });
            await context.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return await GetAccount(userId, ct);
    }
}
