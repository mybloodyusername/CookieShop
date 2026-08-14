using CookieShop.App.DTOs.Category;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class CategoryRepository(CookieShopDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyCollection<Category>> GetAll()
    {
        return await context.Categories.AsNoTracking().ToListAsync();
    }

    public async Task<Category?> GetById(Guid id)
    {
        return await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category> Create(CreateCategoryRequest request)
    {
        var result = await context.Categories.AddAsync(new Category
        {
            Name = request.Name,
        });
        await context.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<Category?> Update(UpdateCategoryRequest request)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id);
        if (category is null) return null;

        category.Name = request.Name;

        await context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> Delete(Guid id)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return false;

        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return true;
    }
}
