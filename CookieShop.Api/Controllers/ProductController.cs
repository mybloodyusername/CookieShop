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
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(
            Guid? categoryId, string? search, int page = 1, int pageSize = 10)
        {
            var result = await productService.GetAll(categoryId, search, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDetailResponse>> GetById(Guid id)
        {
            var result = await productService.GetById(id);
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
    }
}
