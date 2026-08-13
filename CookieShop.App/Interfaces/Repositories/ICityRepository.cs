using CookieShop.App.DTOs.City;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface ICityRepository
{
    public Task<City?> GetById(Guid id);
    public Task<City> Create(CreateCityRequest request);
    public Task<City> Update(UpdateCityRequest request);
    public Task<City> Delete(string id);
}