using CookieShop.App.DTOs.Address;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class AddressRepository(CookieShopDbContext context) : IAddressRepository
{
    public async Task<Address?> GetById(Guid id)
    {
        return await context.Addresses.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<ICollection<Address>> GetAllByUserId(Guid id)
    {
        return await context.Addresses.Where(a => a.UserId == id).ToListAsync();
    }

    public async Task<Address?> Create(CreateAddressRequest request)
    {
        var result = await context.Addresses.AddAsync(new Address
        {
            Title = request.Title,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            PhoneNumber = request.PhoneNumber,
            UserId = request.UserId,
            ProvinceId = request.ProvinceId,
            CityId = request.CityId,
        });
        await context.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<Address?> Update(UpdateAddressRequest request)
    {
        var address = await context.Addresses.FirstOrDefaultAsync(a => a.Id == request.Id);
        if (address is null) return null;

        address.Title = request.Title;
        address.AddressLine1 = request.AddressLine1;
        address.AddressLine2 = request.AddressLine2;
        address.PhoneNumber = request.PhoneNumber;
        address.UserId = request.UserId;
        address.ProvinceId = request.ProvinceId;
        address.CityId = request.CityId;

        await context.SaveChangesAsync();
        return address;
    }

    public async Task<bool> Delete(Guid id)
    {
        var address = await context.Addresses.FirstOrDefaultAsync(p => p.Id == id);
        if (address is null) return false;

        context.Addresses.Remove(address);
        await context.SaveChangesAsync();
        return true;
    }
}