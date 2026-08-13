using CookieShop.App.DTOs.Address;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;

namespace CookieShop.Infra.Repositories;

public class AddressRepository : IAddressRepository
{
    public Task<Address> GetById(string id)
    {
        throw new NotImplementedException();
    }

    public Task<Address> GetAllByUserId(string id)
    {
        throw new NotImplementedException();
    }

    public Task<Address> Create(CreateAddressRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Address> Update(UpdateAddressRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Address> Delete(string id)
    {
        throw new NotImplementedException();
    }
}