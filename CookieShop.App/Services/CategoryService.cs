using CookieShop.App.DTOs.Category;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CookieShop.App.Services;

public class CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
{
    public async Task<IReadOnlyCollection<CategoryResponse>> GetAll()
    {
        var result = await categoryRepository.GetAll();
        return result.Adapt<IReadOnlyCollection<CategoryResponse>>();
    }

    public async Task<CategoryResponse> Create(CreateCategoryRequest request)
    {
        try
        {
            var result = await categoryRepository.Create(request);
            return result.Adapt<CategoryResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to create category with name {Name}", request.Name);
            throw new ConflictException("Failed to create category");
        }
    }

    public async Task<CategoryResponse> Update(UpdateCategoryRequest request)
    {
        try
        {
            var result = await categoryRepository.Update(request);
            if (result is null) throw new NotFoundException("Category not found");
            return result.Adapt<CategoryResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to update category with {CategoryId}", request.Id);
            throw new ConflictException("Failed to update category");
        }
    }

    public async Task<bool> Delete(Guid id)
    {
        try
        {
            if (!await categoryRepository.Delete(id))
                throw new NotFoundException("Category not found.");
            return true;
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to delete category with {CategoryId}", id);
            throw new ConflictException("Category cannot be deleted because products reference it.");
        }
    }
}
