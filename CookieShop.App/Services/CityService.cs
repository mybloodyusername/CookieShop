using CookieShop.App.DTOs.City;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CookieShop.App.Services;

public class CityService(ICityRepository cityRepository, ILogger<CityService> logger)
{
    public async Task<CityResponse> Create(CreateCityRequest request)
    {
        try
        {
            var result = await cityRepository.Create(request);
            return result.Adapt<CityResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to create city.");
            throw new ConflictException("City cannot be created. Make sure the province exists.");
        }
    }

    public async Task<CityResponse> Update(UpdateCityRequest request)
    {
        try
        {
            var result = await cityRepository.Update(request);
            if (result is null) throw new NotFoundException("City not found.");
            return result.Adapt<CityResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to update city.");
            throw new ConflictException("City cannot be updated. Make sure the province exists.");
        }
    }

    public async Task<bool> Delete(Guid id)
    {
        try
        {
            if (!await cityRepository.Delete(id))
                throw new NotFoundException("City not found.");
            return true;
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to delete city.");
            throw new ConflictException("City cannot be deleted because addresses reference it.");
        }
    }

    public async Task<ICollection<CityResponse>> GetByProvinceId(Guid id)
    {
        try
        {
            var result = await cityRepository.GetByProvinceId(id);
            return result.Adapt<List<CityResponse>>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to fetch cities.");
            throw new ConflictException("Cities cannot be fetched. Make sure the province exists.");
        }
    }
}