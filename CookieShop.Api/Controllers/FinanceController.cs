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
    [Authorize(Roles = UserRole.Admin), HttpPost("customers/{userId:guid}/payments")]
    public Task<AccountResponse> Payment(Guid userId, RecordPaymentRequest request, CancellationToken ct) => finance.RecordPayment(userId, User.GetUserId(), request, ct);
}
