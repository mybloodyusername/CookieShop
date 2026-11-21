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
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetAll([FromQuery] bool isDeleted = false)
        {
            if (isDeleted && !User.IsInRole(UserRole.Admin)) isDeleted = false;
            var result = await categoryService.GetAll(isDeleted);
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

        [Authorize(Roles = UserRole.Admin)]
        [HttpPut("SetDeletedByAdmin/{id}")]
        public async Task<ActionResult> SetDeletedByAdmin(Guid id, [FromQuery] bool isDeleted)
        {
            return Ok(new { success = await categoryService.SetDeleted(id, isDeleted) });
        }
    }
}
