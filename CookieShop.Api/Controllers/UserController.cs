using System.Security.Claims;
using CookieShop.App.DTOs.User;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Authorize(Roles = UserRole.Customer)]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController(UserService userService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UserResponse>> Update([FromBody] UpdateUserRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await userService.Update(userId, request);
            return Ok(result);
        }

        [HttpPost]
        public Task<ActionResult> ChangePassword()
        {
            throw new NotImplementedException();
        }
        
        [HttpGet]
        public async Task<ActionResult<UserResponse>> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await userService.Me(userId);
            return Ok(result);
        }
    }
}