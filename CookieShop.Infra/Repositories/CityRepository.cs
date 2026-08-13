using CookieShop.App.DTOs.City;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;

namespace CookieShop.Infra.Repositories;

public class CityRepository(CookieShopDbContext context) : ICityRepository
{
    public Task<City?> GetById(string id)
    {
        throw new NotImplementedException();
    }

    public Task<City> Create(CreateCityRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<City> Update(UpdateCityRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<City> Delete(string id)
    {
        throw new NotImplementedException();
    }
}