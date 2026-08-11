using CookieShop.App.DTOs.User;
using CookieShop.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(UserService userService) : ControllerBase
    {
        [HttpPost("[action]")]
        public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
        {
            var result = await userService.Create(request);
            return Ok(result);
        }
    }
}