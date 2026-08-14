using CookieShop.App.DTOs.Address;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CookieShop.App.Services;

public class AddressService(IAddressRepository addressRepository, ILogger<AddressService> logger)
{
    public async Task<IReadOnlyCollection<AddressResponse>> GetAllByUserId(Guid userId)
    {
        var result = await addressRepository.GetAllByUserId(userId);
        return result.Adapt<IReadOnlyCollection<AddressResponse>>();
    }

    public async Task<AddressResponse> Create(CreateAddressRequest request)
    {
        try
        {
            var result = await addressRepository.Create(request);
            return result.Adapt<AddressResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError("Failed to create address for user {UserId}", request.UserId);
            throw new ConflictException("Failed to create address");
        }
    }

    public async Task<AddressResponse> Update(Guid userId, UpdateAddressRequest request)
    {
        try
        {
            if (request.UserId != userId) throw new UnauthorizedAccessException();
            var result = await addressRepository.Update(request);
            if (result is null) throw new NotFoundException("Address not found");
            return result.Adapt<AddressResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError("Failed to update address with {AddressId} for user {UserId}", request.Id, request.UserId);
            throw new ConflictException("Failed to update address");
        }
    }

    public async Task<bool> Delete(Guid id)
    {
        try
        {
            if (!await addressRepository.Delete(id))
                throw new NotFoundException("Address not found.");
            return true;
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to delete address.");
            throw new ConflictException("Address cannot be deleted because orders reference it.");
        }
    }
}