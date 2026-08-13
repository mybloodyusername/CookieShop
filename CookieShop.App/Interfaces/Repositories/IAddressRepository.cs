using CookieShop.App.DTOs.Address;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface IAddressRepository
{
    public Task<Address> GetById(string id);
    public Task<Address> GetAllByUserId(string id);
    public Task<Address> Create(CreateAddressRequest request);
    public Task<Address> Update(UpdateAddressRequest request);
    public Task<Address> Delete(string id);
}