using CookieShop.App.DTOs.Finance;
using CookieShop.App.Extensions;
using CookieShop.Domain.Constants;
using CookieShop.Infra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CookieShop.Api.Controllers;
[ApiController, Route("api/finance"), Authorize]
public class FinanceController(CustomerFinanceService finance) : ControllerBase
{
    [Authorize(Policy = "CustomerOnly"), HttpGet("account")]
    public Task<AccountResponse> Account(CancellationToken ct) => finance.GetAccount(User.GetUserId(), ct);
    [Authorize(Policy = "CustomerOnly"), HttpGet("history")]
    public Task<IReadOnlyCollection<FinancialEntryResponse>> History(CancellationToken ct, int page = 1) => finance.History(User.GetUserId(), page, ct);
    [Authorize(Roles = UserRole.Admin), HttpGet("customers/{userId:guid}")]
    public Task<AccountResponse> Customer(Guid userId, CancellationToken ct) => finance.GetAccount(userId, ct);
    [Authorize(Roles = UserRole.Admin), HttpGet("customers/{userId:guid}/history")]
    public Task<IReadOnlyCollection<FinancialEntryResponse>> CustomerHistory(Guid userId, CancellationToken ct, int page = 1) => finance.History(userId, page, ct);
    [Authorize(Roles = UserRole.Admin), HttpPut("customers/{userId:guid}/terms")]
    public Task<AccountResponse> Terms(Guid userId, PaymentTermsRequest request, CancellationToken ct) => finance.SetTerms(userId, request, ct);
    /// <summary>Records an external receipt and explicitly allocates it to selected orders.</summary>
    [Authorize(Roles = UserRole.Admin), HttpPost("customers/{userId:guid}/payments")]
    public async Task<ActionResult<FinancialEntryResponse>> Payment(Guid userId, RecordPaymentRequest request, CancellationToken ct)
    {
        var entry = await finance.RecordPayment(userId, User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Entry), new { userId, entryId = entry.Id }, entry);
    }
    /// <summary>Records money already returned to a customer by cash, transfer, or another external method.</summary>
    [Authorize(Roles = UserRole.Admin), HttpPost("customers/{userId:guid}/refunds")]
    public async Task<ActionResult<FinancialEntryResponse>> Refund(Guid userId, RecordRefundRequest request, CancellationToken ct)
    {
        var entry = await finance.RecordRefund(userId, User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Entry), new { userId, entryId = entry.Id }, entry);
    }
    /// <summary>Returns one customer receipt, refund, or ledger posting with its order allocations.</summary>
    [Authorize(Roles = UserRole.Admin), HttpGet("customers/{userId:guid}/entries/{entryId:guid}")]
    public Task<FinancialEntryResponse> Entry(Guid userId, Guid entryId, CancellationToken ct) => finance.GetEntry(userId, entryId, ct);
}
