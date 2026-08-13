using CookieShop.App.DTOs.Province;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class ProvinceRepository(CookieShopDbContext context) : IProvinceRepository
{
    public async Task<Province?> GetById(Guid id)
    {
        return await context.Provinces
            .Include(p => p.Cities)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ICollection<Province>> GetAll()
    {
        return await context.Provinces.ToListAsync();
    }

    public async Task<Province> Create(CreateProvinceRequest request)
    {
        var result = await context.AddAsync(new Province
        {
            Name = request.Name
        });
        await context.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<Province?> Update(UpdateProvinceRequest request)
    {
        var province = await context.Provinces.FirstOrDefaultAsync(p => p.Id == request.Id);
        if (province is null) return null;

        province.Name = request.Name;
        await context.SaveChangesAsync();
        return province;
    }

    public async Task<bool> Delete(Guid id)
    {
        var province = await context.Provinces.FirstOrDefaultAsync(p => p.Id == id);
        if (province is null) return false;

        context.Provinces.Remove(province);
        await context.SaveChangesAsync();
        return true;
    }
}