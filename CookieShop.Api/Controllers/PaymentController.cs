using CookieShop.App.DTOs.Finance;
using CookieShop.App.Extensions;
using CookieShop.App.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CookieShop.Api.Controllers;

[ApiController, Route("api/payments")]
public sealed class PaymentController(
    IOrderPaymentService payments,
    IHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Applies available wallet credit to the customer's order.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("orders/{orderId:guid}/wallet")]
    public Task<OrderPaymentResponse> Wallet(Guid orderId, CancellationToken ct) => payments.PayWithWallet(User.GetUserId(), orderId, ct);

    /// <summary>Creates a checkout using a configured gateway adapter.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("orders/{orderId:guid}/gateway")]
    public async Task<ActionResult<PaymentCheckoutResponse>> Start(Guid orderId, StartPaymentRequest request, CancellationToken ct)
    {
        var checkout = await payments.Start(User.GetUserId(), orderId, request.Provider, ApiBaseUrl(), ct);
        return Created($"/api/payments/{checkout.AttemptId}/verify", checkout);
    }

    /// <summary>Starts a wallet top-up before order creation using a configured gateway adapter.</summary>
    [Authorize(Policy = "CustomerOnly"), HttpPost("wallet/gateway")]
    public async Task<ActionResult<PaymentCheckoutResponse>> TopUp(StartWalletTopUpRequest request, CancellationToken ct)
    {
        var checkout = await payments.StartWalletTopUp(User.GetUserId(), request, ApiBaseUrl(), ct);
        return Created($"/api/payments/{checkout.AttemptId}/verify", checkout);
    }

    /// <summary>Development-only mock checkout page with explicit success and rejection actions.</summary>
    [AllowAnonymous, HttpGet("mock-checkout/{attemptId:guid}")]
    public IActionResult MockCheckout(Guid attemptId)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var id = attemptId.ToString("D");
        var html = $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>CookieShop mock payment</title>
              <style>
                * { box-sizing: border-box; }
                body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 24px; background: #f5f3f0; color: #25221f; font: 16px/1.5 system-ui, sans-serif; }
                main { width: min(100%, 520px); padding: 28px; border: 1px solid #d8d1c9; border-radius: 16px; background: #fff; box-shadow: 0 12px 36px #25221f14; }
                h1 { margin: 0 0 8px; font-size: 1.5rem; }
                p { color: #5e5851; }
                .actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 24px; }
                button { min-height: 48px; padding: 0 18px; border: 0; border-radius: 8px; color: #fff; font: inherit; font-weight: 650; cursor: pointer; }
                .success { background: #267348; }
                .reject { background: #a83a35; }
                button:focus-visible { outline: 3px solid #6d4b9b; outline-offset: 3px; }
                @media (max-width: 480px) { .actions { display: grid; } }
              </style>
            </head>
            <body>
              <main>
                <h1>CookieShop payment simulator</h1>
                <p>This development-only page does not contact a payment provider. Choose an outcome to continue.</p>
                <div class="actions">
                  <form method="post" action="/api/payments/mock-checkout/{{id}}/success">
                    <button class="success" type="submit">Simulate successful payment</button>
                  </form>
                  <form method="post" action="/api/payments/mock-checkout/{{id}}/reject">
                    <button class="reject" type="submit">Simulate rejected payment</button>
                  </form>
                </div>
              </main>
            </body>
            </html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [AllowAnonymous, IgnoreAntiforgeryToken, HttpPost("mock-checkout/{attemptId:guid}/success")]
    public Task<IActionResult> MockSuccess(Guid attemptId, CancellationToken ct) => MockOutcome(attemptId, true, ct);

    [AllowAnonymous, IgnoreAntiforgeryToken, HttpPost("mock-checkout/{attemptId:guid}/reject")]
    public Task<IActionResult> MockReject(Guid attemptId, CancellationToken ct) => MockOutcome(attemptId, false, ct);

    /// <summary>Asks the configured provider to verify the stored attempt. Callback claims are never trusted.</summary>
    [AllowAnonymous, IgnoreAntiforgeryToken, HttpPost("{attemptId:guid}/verify")]
    public async Task<IActionResult> Verify(Guid attemptId, CancellationToken ct)
    {
        await payments.Verify(attemptId, ct);
        return NoContent();
    }

    private string ApiBaseUrl() => $"{Request.Scheme}://{Request.Host}";

    private async Task<IActionResult> MockOutcome(Guid attemptId, bool succeeded, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        await payments.ChooseMockOutcome(attemptId, succeeded, ct);
        var configuredPortalUrl = configuration["MockCheckout:PortalBaseUrl"] ?? "http://localhost:4201";
        if (!Uri.TryCreate(configuredPortalUrl, UriKind.Absolute, out var portalUri) ||
            portalUri.Scheme is not ("http" or "https"))
            return StatusCode(StatusCodes.Status500InternalServerError);
        var outcome = succeeded ? "success" : "rejected";
        return Redirect(new Uri(portalUri, $"/payment/result?attemptId={attemptId:D}&mockOutcome={outcome}").ToString());
    }
}
