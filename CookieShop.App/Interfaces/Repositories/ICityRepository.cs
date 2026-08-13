using CookieShop.App.DTOs.City;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface ICityRepository
{
    public Task<City?> GetById(Guid id);
    public Task<ICollection<City>> GetByProvinceId(Guid id);
    public Task<City> Create(CreateCityRequest request);
    public Task<City?> Update(UpdateCityRequest request);
    public Task<bool> Delete(Guid id);
}