using CookieShop.App.DTOs.Common;
using CookieShop.App.DTOs.Product;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface IProductRepository
{
    public Task<Product?> GetById(Guid id, bool includeDeleted = false);
    public Task<PagedResult<Product>> GetFiltered(Guid? categoryId, string? search, bool isDeleted, bool includeUnavailable, int page, int pageSize);
    public Task<Product> Create(CreateProductRequest request);
    public Task<Product?> Update(UpdateProductRequest request);
    public Task<bool> Delete(Guid id);
    public Task<bool> SetDeleted(Guid id, bool isDeleted);
}
