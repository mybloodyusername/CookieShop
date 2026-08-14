using CookieShop.App.DTOs.Category;
using CookieShop.App.Services;
using CookieShop.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(CategoryService categoryService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetAll()
        {
            var result = await categoryService.GetAll();
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPost("CreateByAdmin")]
        public async Task<ActionResult<CategoryResponse>> CreateByAdmin([FromBody] CreateCategoryRequest category)
        {
            var result = await categoryService.Create(category);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpPut("UpdateByAdmin")]
        public async Task<ActionResult<CategoryResponse>> UpdateByAdmin([FromBody] UpdateCategoryRequest category)
        {
            var result = await categoryService.Update(category);
            return Ok(result);
        }

        [Authorize(Roles = UserRole.Admin)]
        [HttpDelete("DeleteByAdmin/{id}")]
        public async Task<ActionResult> DeleteByAdmin(Guid id)
        {
            var result = await categoryService.Delete(id);
            return Ok(new { success = result });
        }
    }
}
