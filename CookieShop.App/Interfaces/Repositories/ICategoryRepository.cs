using CookieShop.App.DTOs.Category;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface ICategoryRepository
{
    public Task<IReadOnlyCollection<Category>> GetAll(bool includeDeleted);
    public Task<bool> HasProducts(Guid id);
    public Task<bool> SetDeleted(Guid id, bool isDeleted);
    public Task<Category?> GetById(Guid id);
    public Task<Category> Create(CreateCategoryRequest request);
    public Task<Category?> Update(UpdateCategoryRequest request);
    public Task<bool> Delete(Guid id);
}
