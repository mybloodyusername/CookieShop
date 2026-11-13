using CookieShop.App.DTOs.User;
using CookieShop.App.Extensions;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(UserService userService) : ControllerBase
    {
        /// <summary>Checks whether a phone number can be used for a new account.</summary>
        [HttpGet("PhoneNumberAvailability")]
        public async Task<ActionResult<AvailabilityResponse>> PhoneNumberAvailability(
            [FromQuery, Required, RegularExpression(@"^09[0-9]{9}$")] string phoneNumber)
        {
            return Ok(new AvailabilityResponse(await userService.IsPhoneNumberAvailable(phoneNumber)));
        }

        /// <summary>Checks whether an email address can be used for a new account.</summary>
        [HttpGet("EmailAvailability")]
        public async Task<ActionResult<AvailabilityResponse>> EmailAvailability(
            [FromQuery, Required, EmailAddress, MaxLength(256)] string email)
        {
            return Ok(new AvailabilityResponse(await userService.IsEmailAvailable(email)));
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
        public async Task<ActionResult> GetAll(int page = 1, int pageSize = 30)
        {
            return Ok(await userService.GetAll(page, pageSize));
        }
    }
}
