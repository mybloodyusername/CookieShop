using CookieShop.App.DTOs.Auth;
using CookieShop.App.DTOs.User;
using CookieShop.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using CookieShop.App.Extensions;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController(AuthService authService) : ControllerBase
    {
        [AllowAnonymous, HttpGet]
        public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
        }

        [Authorize, HttpPost]
        public async Task<IActionResult> Logout()
        {
            await authService.Logout(User.GetUserId());
            return NoContent();
        }

        [AllowAnonymous, EnableRateLimiting("auth"), HttpPost]
        public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
        {
            return await authService.Login(request);
        }

        [AllowAnonymous, EnableRateLimiting("auth"), HttpPost]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            return await authService.Register(request);
        }
    }
}