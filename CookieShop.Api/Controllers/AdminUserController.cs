using CookieShop.App.DTOs.User;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/Admin/[controller]/[action]")]
    [Authorize(Roles = UserRole.Admin)]
    [ApiController]
    public class AdminUserController(UserService userService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
        {
            var result = await userService.Create(request);
            return Ok(result);
        }

        [HttpPost("{userId}")]
        public async Task<ActionResult<UserResponse>> Update(string userId, [FromBody] UpdateUserRequest request)
        {
            var result = await userService.Update(userId, request);
            return Ok(result);
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserResponse>> GetById(string userId)
        {
            var result = await userService.GetById(userId);
            return Ok(result);
        }
        
        [HttpGet("{userPhoneNumber}")]
        public async Task<ActionResult<UserResponse>> GetByPhoneNumber(string userPhoneNumber)
        {
            var result = await userService.GetByPhoneNumber(userPhoneNumber);
            return Ok(result);
        }

        [HttpGet]
        public Task<ActionResult> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}