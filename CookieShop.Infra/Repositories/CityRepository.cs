using CookieShop.App.DTOs.City;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class CityRepository(CookieShopDbContext context) : ICityRepository
{
    public async Task<City?> GetById(Guid id)
    {
        return await context.Cities.FirstOrDefaultAsync(c => c.Id == id);
    }

    public Task<City> Create(CreateCityRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<City> Update(UpdateCityRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<City> Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}