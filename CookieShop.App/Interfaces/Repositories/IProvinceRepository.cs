using CookieShop.App.DTOs.Province;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface IProvinceRepository
{
    public Task<Province?> GetById(Guid id);
    public Task<Province?> Create(CreateProvinceRequest request);
    public Task<Province?> Update(UpdateProvinceRequest request);
    public Task<bool> Delete(Guid id);
}