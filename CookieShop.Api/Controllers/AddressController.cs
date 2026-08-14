using CookieShop.App.DTOs.Address;
using CookieShop.App.Extensions;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController(AddressService addressService) : ControllerBase
    {
        [Authorize(Roles = UserRole.Customer)]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<AddressResponse>>> GetAllByUserId()
        {
            var userId = User.GetUserId();
            var result = await addressService.GetAllByUserId(userId);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Customer)]
        [HttpPost("Create")]
        public async Task<ActionResult<AddressResponse>> Create([FromBody] CreateAddressRequest address)
        {
            var userId = User.GetUserId();
            if (address.UserId != userId) throw new UnauthorizedAccessException();
            var result = await addressService.Create(address);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Customer)]
        [HttpPut("Update")]
        public async Task<ActionResult<AddressResponse>> Update([FromBody] UpdateAddressRequest address)
        {
            var userId = User.GetUserId();
            var result = await addressService.Update(userId, address);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Customer)]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await addressService.Delete(id);
            return Ok(new { success = result });
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpGet("GetAllByUserIdByAdmin/{userId}")]
        public async Task<ActionResult<IReadOnlyCollection<AddressResponse>>> GetAllByUserIdByAdmin(Guid userId)
        {
            var result = await addressService.GetAllByUserId(userId);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPost("CreateByAdmin")]
        public async Task<ActionResult<AddressResponse>> CreateByAdmin([FromBody] CreateAddressRequest address)
        {
            var result = await addressService.Create(address);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPut("UpdateByAdmin")]
        public async Task<ActionResult<AddressResponse>> UpdateByAdmin([FromBody] UpdateAddressRequest address)
        {
            var result = await addressService.Update(address.UserId, address);
            return Ok(result);
        }
    }
}