using CookieShop.App.DTOs.Province;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CookieShop.App.Services;

public class ProvinceService(IProvinceRepository provinceRepository, ILogger<ProvinceService> logger)
{
    public async Task<ProvinceDetailResponse> GetById(Guid id)
    {
        var result = await provinceRepository.GetById(id);
        if (result is null) throw new NotFoundException("Province not found.");
        return result.Adapt<ProvinceDetailResponse>();
    }

    public async Task<ProvinceResponse> Create(CreateProvinceRequest request)
    {
        try
        {
            var province = await provinceRepository.Create(request);
            return province.Adapt<ProvinceResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to create province.");
            throw new ConflictException("Province cannot be created.");
        }
    }

    public async Task<ProvinceResponse> Update(UpdateProvinceRequest request)
    {
        try
        {
            var province = await provinceRepository.Update(request);
            if (province is null) throw new NotFoundException("Province not found.");
            return province.Adapt<ProvinceResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to update province.");
            throw new ConflictException("Province cannot be updated.");
        }
    }

    public async Task<bool> Delete(Guid id)
    {
        try
        {
            if (!await provinceRepository.Delete(id)) 
                throw new NotFoundException("Province not found.");
            return true;
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to delete province.");
            throw new ConflictException("Province cannot be deleted.");
        }
    }

    public async Task<IReadOnlyCollection<ProvinceResponse>> GetAll()
    {
        var provinces = await provinceRepository.GetAll();
        return provinces.Adapt<List<ProvinceResponse>>();
    }
}