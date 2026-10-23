using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CookieShop.Api.Security;

// Cookie-backed API sessions need CSRF protection, including login and registration.
// Implement this directly so an API-only host does not need MVC view services.
public sealed class CsrfValidationFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method)) return;
        try { await antiforgery.ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            { Status = 400, Title = "Invalid CSRF token", Detail = "Fetch a current token from /api/Auth/Csrf and send it in X-CSRF-TOKEN." });
        }
    }
}
