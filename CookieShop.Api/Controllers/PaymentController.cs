using CookieShop.App.DTOs.Finance;
using CookieShop.App.Extensions;
using CookieShop.App.Interfaces;
using CookieShop.App.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CookieShop.Api.Controllers;

[ApiController, Route("api/payments")]
public sealed class PaymentController(IOrderPaymentService payments, OrderService orders, IHostEnvironment environment) : ControllerBase
{
    /// <summary>Applies available wallet credit to the customer's order.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("orders/{orderId:guid}/wallet")]
    public Task<OrderPaymentResponse> Wallet(Guid orderId, CancellationToken ct) => payments.PayWithWallet(User.GetUserId(), orderId, ct);

    /// <summary>Creates a checkout using a configured gateway adapter.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("orders/{orderId:guid}/gateway")]
    public async Task<ActionResult<PaymentCheckoutResponse>> Start(Guid orderId, StartPaymentRequest request, CancellationToken ct)
    {
        if (environment.IsDevelopment() && request.Provider == "zarinpal-mock")
        {
            var order = await orders.GetById(User.GetUserId(), orderId);
            if (order.AmountDue <= 0) return Conflict();

            // TODO: gateway implementation — replace this development-only acknowledgement with a ZarinPal adapter.
            // ZarinPal code 100 acknowledges a payment request; it does not mean the order was paid.
            var mock = new MockZarinPalResponse(
                new MockZarinPalData(100, "Success", $"MOCK{Guid.NewGuid():N}", "Merchant", 0),
                []);
            return Ok(new PaymentCheckoutResponse(null, null, mock));
        }

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
