using CookieShop.App.DTOs.Category;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface ICategoryRepository
{
    public Task<IReadOnlyCollection<Category>> GetAll();
    public Task<Category?> GetById(Guid id);
    public Task<Category> Create(CreateCategoryRequest request);
    public Task<Category?> Update(UpdateCategoryRequest request);
    public Task<bool> Delete(Guid id);
}
