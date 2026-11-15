using CookieShop.App.DTOs.Cart;
using CookieShop.App.Extensions;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController(CartService cartService) : ControllerBase
    {
        [Authorize(Policy = "CustomerOnly")]
        [HttpGet]
        public async Task<ActionResult<CartResponse>> Get()
        {
            var userId = User.GetUserId();
            var result = await cartService.GetCart(userId);
            return Ok(result);
        }

        [Authorize(Policy = "CustomerOnly")]
        [HttpPost("AddItem")]
        public async Task<ActionResult<CartResponse>> AddItem([FromBody] AddCartItemRequest request)
        {
            var userId = User.GetUserId();
            var result = await cartService.AddItem(userId, request);
            return Ok(result);
        }

        [Authorize(Policy = "CustomerOnly")]
        [HttpPut("UpdateItem")]
        public async Task<ActionResult<CartResponse>> UpdateItem([FromBody] UpdateCartItemRequest request)
        {
            var userId = User.GetUserId();
            var result = await cartService.UpdateItemQuantity(userId, request);
            return Ok(result);
        }

        [Authorize(Policy = "CustomerOnly")]
        [HttpDelete("RemoveItem/{productId}")]
        public async Task<ActionResult<CartResponse>> RemoveItem(Guid productId)
        {
            var userId = User.GetUserId();
            var result = await cartService.RemoveItem(userId, productId);
            return Ok(result);
        }

        [Authorize(Policy = "CustomerOnly")]
        [HttpDelete("Clear")]
        public async Task<ActionResult<CartResponse>> Clear()
        {
            var userId = User.GetUserId();
            var result = await cartService.Clear(userId);
            return Ok(result);
        }
    }
}
