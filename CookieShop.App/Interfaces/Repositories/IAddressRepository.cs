using CookieShop.App.DTOs.Address;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface IAddressRepository
{
    public Task<Address?> GetById(Guid id);
    public Task<IReadOnlyCollection<Address>> GetAllByUserId(Guid id);
    public Task<Address> Create(CreateAddressRequest request);
    public Task<Address?> Update(UpdateAddressRequest request);
    public Task<bool> Delete(Guid id);
}