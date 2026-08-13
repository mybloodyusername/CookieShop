using CookieShop.App.DTOs.User;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers.Admin
{
    [Authorize(Roles = UserRole.Admin)]
    [Route("api/Admin/User/[action]")]
    [ApiController]
    public class AdminUserController(UserService userService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
        {
            var result = await userService.Create(request);
            return Ok(result);
        }

        [HttpPost("{id}")]
        public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var result = await userService.Update(id, request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetById(Guid id)
        {
            var result = await userService.GetById(id);
            return Ok(result);
        }
        
        [HttpGet("{phoneNumber}")]
        public async Task<ActionResult<UserResponse>> GetByPhoneNumber(string phoneNumber)
        {
            var result = await userService.GetByPhoneNumber(phoneNumber);
            return Ok(result);
        }

        [HttpGet]
        public Task<ActionResult> GetAll()
        {
            throw new NotImplementedException();
        }
    }
}