using CookieShop.App.DTOs.Common;
using CookieShop.App.DTOs.Order;
using CookieShop.App.Extensions;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using CookieShop.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(OrderService orderService) : ControllerBase
    {
        [Authorize(Roles = UserRole.Customer)]
        [HttpPost("Create")]
        public async Task<ActionResult<OrderDetailResponse>> Create([FromBody] CreateOrderRequest request)
        {
            var userId = User.GetUserId();
            var result = await orderService.PlaceOrder(userId, request);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Customer)]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<OrderResponse>>> GetMyOrders()
        {
            var userId = User.GetUserId();
            var result = await orderService.GetMyOrders(userId);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Customer)]
        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<OrderDetailResponse>> GetById(Guid id)
        {
            var userId = User.GetUserId();
            var result = await orderService.GetById(userId, id);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Customer)]
        [HttpPut("Cancel/{id}")]
        public async Task<ActionResult<OrderResponse>> Cancel(Guid id)
        {
            var userId = User.GetUserId();
            var result = await orderService.Cancel(userId, id);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpGet("GetAllByAdmin")]
        public async Task<ActionResult<PagedResult<OrderResponse>>> GetAllByAdmin(
            OrderStatus? status, int page = 1, int pageSize = 10)
        {
            var result = await orderService.GetAllByAdmin(status, page, pageSize);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpGet("GetByIdByAdmin/{id}")]
        public async Task<ActionResult<OrderDetailResponse>> GetByIdByAdmin(Guid id)
        {
            var result = await orderService.GetByIdByAdmin(id);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPut("UpdateStatusByAdmin")]
        public async Task<ActionResult<OrderResponse>> UpdateStatusByAdmin([FromBody] UpdateOrderStatusRequest request)
        {
            var result = await orderService.UpdateStatusByAdmin(request);
            return Ok(result);
        }
    }
}
