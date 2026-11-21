using CookieShop.App.DTOs.Common;
using CookieShop.App.DTOs.Product;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(ProductService productService) : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
            Guid? categoryId, string? search, [FromQuery] bool isDeleted = false, int page = 1, int pageSize = 10)
        {
            if (isDeleted && !User.IsInRole(UserRole.Admin)) isDeleted = false;
            // TODO: remove
            await Task.Delay(1000);
            var result = await productService.GetAll(categoryId, search, isDeleted, User.IsInRole(UserRole.Admin), page, pageSize);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDetailResponse>> GetById(Guid id)
        {
            // TODO: remove
            await Task.Delay(1000);
            var result = await productService.GetById(id, User.IsInRole(UserRole.Admin));
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPost("CreateByAdmin")]
        public async Task<ActionResult<ProductResponse>> CreateByAdmin([FromBody] CreateProductRequest product)
        {
            var result = await productService.Create(product);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPut("UpdateByAdmin")]
        public async Task<ActionResult<ProductResponse>> UpdateByAdmin([FromBody] UpdateProductRequest product)
        {
            var result = await productService.Update(product);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpDelete("DeleteByAdmin/{id}")]
        public async Task<ActionResult> DeleteByAdmin(Guid id)
        {
            var result = await productService.Delete(id);
            return Ok(new { success = result });
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPut("SetDeletedByAdmin/{id}")]
        public async Task<ActionResult> SetDeletedByAdmin(Guid id, [FromQuery] bool isDeleted)
        {
            return Ok(new { success = await productService.SetDeleted(id, isDeleted) });
        }
    }
}
