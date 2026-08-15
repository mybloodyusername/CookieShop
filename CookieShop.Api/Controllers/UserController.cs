using CookieShop.App.DTOs.User;
using CookieShop.App.Extensions;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(UserService userService) : ControllerBase
    {
        [Authorize(Roles = UserRole.Customer)]
        [HttpPut("Update")]
        public async Task<ActionResult<UserResponse>> Update([FromBody] UpdateUserRequest request)
        {
            var userId = User.GetUserId();
            var result = await userService.Update(userId, request);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Customer)]
        [HttpPost("ChangePassword")]
        public Task<ActionResult> ChangePassword()
        {
            throw new NotImplementedException();
        }

        [Authorize]
        [HttpGet("Me")]
        public async Task<ActionResult<UserResponse>> Me()
        {
            var userId = User.GetUserId();
            var role = User.GetRole();
            var result = await userService.Me(userId);
            result.Role = role;
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPost("CreateByAdmin")]
        public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
        {
            var result = await userService.Create(request);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPost("UpdateByAdmin/{id}")]
        public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var result = await userService.Update(id, request);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpGet("GetByIdByAdmin/{id}")]
        public async Task<ActionResult<UserResponse>> GetById(Guid id)
        {
            var result = await userService.GetById(id);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpGet("GetByPhoneNumberByAdmin/{phoneNumber}")]
        public async Task<ActionResult<UserResponse>> GetByPhoneNumber(string phoneNumber)
        {
            var result = await userService.GetByPhoneNumber(phoneNumber);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpGet("GetAllByAdmin")]
        public Task<ActionResult> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}