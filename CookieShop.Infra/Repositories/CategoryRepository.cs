using CookieShop.App.DTOs.Category;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class CategoryRepository(CookieShopDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyCollection<Category>> GetAll(bool includeDeleted)
    {
        IQueryable<Category> query = context.Categories.AsNoTracking();
        query = query.Where(category => category.IsDeleted == includeDeleted);
        return await query.Include(category => category.Products).ToListAsync();
    }

    public Task<bool> HasProducts(Guid id) => context.Products.AnyAsync(product => product.CategoryId == id);

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
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted);
        if (category is null) return null;

        category.Name = request.Name;

        await context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> Delete(Guid id)
    {
        return await SetDeleted(id, true);
    }

    public async Task<bool> SetDeleted(Guid id, bool isDeleted)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return false;
        if (isDeleted && await context.Products.AnyAsync(product => product.CategoryId == id))
            throw new CookieShop.App.Exceptions.ConflictException("Category cannot be deleted while products reference it.");
        category.IsDeleted = isDeleted;
        await context.SaveChangesAsync();
        return true;
    }
}
