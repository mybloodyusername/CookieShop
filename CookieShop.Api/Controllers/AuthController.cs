using CookieShop.App.DTOs.Auth;
using CookieShop.App.DTOs.User;
using CookieShop.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController(AuthService authService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UserResponse>> Login([FromBody] LoginRequest request)
        {
            return await authService.Login(request);
        }

        [HttpPost]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            return await authService.Register(request);
        }
    }
}