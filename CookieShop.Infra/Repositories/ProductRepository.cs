using CookieShop.App.DTOs.Common;
using CookieShop.App.DTOs.Product;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class ProductRepository(CookieShopDbContext context) : IProductRepository
{
    public async Task<Product?> GetById(Guid id, bool includeDeleted = false)
    {
        var query = context.Products.AsNoTracking();
        if (!includeDeleted) query = query.Where(product => !product.IsDeleted);
        return await query
            .Include(p => p.Category)
            .Include(p => p.Images).ThenInclude(x => x.Image)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PagedResult<Product>> GetFiltered(Guid? categoryId, string? search, bool isDeleted, bool includeUnavailable, int page, int pageSize)
    {
        var query = context.Products.AsNoTracking().Include(p => p.Images).ThenInclude(x => x.Image)
            .Where(product => product.IsDeleted == isDeleted && (includeUnavailable || product.IsAvailable));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.ToLower().Contains(search.ToLower()) ||
                p.Description.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>(items, totalCount, page, pageSize);
    }

    public async Task<Product> Create(CreateProductRequest request)
    {
        if (!await context.Categories.AnyAsync(category => category.Id == request.CategoryId && !category.IsDeleted))
            throw new CookieShop.App.Exceptions.ConflictException("The selected category is unavailable.");
        var result = await context.Products.AddAsync(new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsOnSale = request.IsOnSale,
            SalePrice = request.SalePrice,
            ImageUrl = request.ImageUrl,
            StockQuantity = request.StockQuantity,
            IsAvailable = request.IsAvailable,
            CategoryId = request.CategoryId,
        });
        await context.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<Product?> Update(UpdateProductRequest request)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == request.Id);
        if (product is null || product.IsDeleted) return null;
        if (!await context.Categories.AnyAsync(category => category.Id == request.CategoryId && !category.IsDeleted))
            throw new CookieShop.App.Exceptions.ConflictException("The selected category is unavailable.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.IsOnSale = request.IsOnSale;
        product.SalePrice = request.SalePrice;
        product.ImageUrl = request.ImageUrl;
        product.StockQuantity = request.StockQuantity;
        product.IsAvailable = request.IsAvailable;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> Delete(Guid id)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (product is null) return false;
        product.IsDeleted = true;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await context.CartItems.Where(item => item.ProductId == id).ExecuteDeleteAsync();
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetDeleted(Guid id, bool isDeleted)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return false;
        if (!isDeleted && await context.Categories.AnyAsync(category => category.Id == product.CategoryId && category.IsDeleted))
            throw new CookieShop.App.Exceptions.ConflictException("A product in a deleted category cannot be restored.");
        product.IsDeleted = isDeleted;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        if (isDeleted) await context.CartItems.Where(item => item.ProductId == id).ExecuteDeleteAsync();
        await context.SaveChangesAsync();
        return true;
    }
}
