using CookieShop.App.DTOs.Finance;
using CookieShop.App.Extensions;
using CookieShop.App.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CookieShop.Api.Controllers;

[ApiController, Route("api/payments")]
public sealed class PaymentController(IOrderPaymentService payments) : ControllerBase
{
    /// <summary>Applies available wallet credit to the customer's order.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("orders/{orderId:guid}/wallet")]
    public Task<OrderPaymentResponse> Wallet(Guid orderId, CancellationToken ct) => payments.PayWithWallet(User.GetUserId(), orderId, ct);

    /// <summary>Creates a checkout using a configured gateway adapter.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("orders/{orderId:guid}/gateway")]
    public async Task<ActionResult<PaymentCheckoutResponse>> Start(Guid orderId, StartPaymentRequest request, CancellationToken ct)
    {
        var checkout = await payments.Start(User.GetUserId(), orderId, request.Provider, ct);
        return Created($"/api/payments/{checkout.AttemptId}/verify", checkout);
    }

    /// <summary>Starts a wallet top-up before order creation using a configured gateway adapter.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("wallet/gateway")]
    public async Task<ActionResult<PaymentCheckoutResponse>> TopUp(StartWalletTopUpRequest request, CancellationToken ct)
    {
        var checkout = await payments.StartWalletTopUp(User.GetUserId(), request, ct);
        return Created($"/api/payments/{checkout.AttemptId}/verify", checkout);
    }

    /// <summary>Asks the configured provider to verify the stored attempt. Callback claims are never trusted.</summary>
    [AllowAnonymous, IgnoreAntiforgeryToken, HttpPost("{attemptId:guid}/verify")]
    public async Task<IActionResult> Verify(Guid attemptId, CancellationToken ct)
    {
        await payments.Verify(attemptId, ct);
        return NoContent();
    }
}
