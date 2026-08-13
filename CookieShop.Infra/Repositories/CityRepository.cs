using CookieShop.App.DTOs.City;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class CityRepository(CookieShopDbContext context) : ICityRepository
{
    public async Task<City?> GetById(Guid id)
    {
        return await context.Cities.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<City> Create(CreateCityRequest request)
    {
        var result = await context.Cities.AddAsync(new City
        {
            Name = request.Name,
            ProvinceId = request.ProvinceId
        });
        await context.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<City?> Update(UpdateCityRequest request)
    {
        var city = await context.Cities.FirstOrDefaultAsync(c => c.Id == request.Id);
        if (city is null) return null;

        city.Name = request.Name;
        city.ProvinceId = request.ProvinceId;

        await context.SaveChangesAsync();
        return city;
    }

    public async Task<bool> Delete(Guid id)
    {
        var city = await context.Cities.FirstOrDefaultAsync(c => c.Id == id);
        if (city is null) return false;

        context.Cities.Remove(city);
        await context.SaveChangesAsync();
        return true;
    }
}