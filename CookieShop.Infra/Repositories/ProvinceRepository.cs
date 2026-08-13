using CookieShop.App.DTOs.Province;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;

namespace CookieShop.Infra.Repositories;

public class ProvinceRepository : IProvinceRepository
{
    public Task<Province> GetById(string id)
    {
        throw new NotImplementedException();
    }

    public Task<Province> Create(CreateProvinceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Province> Update(UpdateProvinceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Province> Delete(string id)
    {
        throw new NotImplementedException();
    }
}