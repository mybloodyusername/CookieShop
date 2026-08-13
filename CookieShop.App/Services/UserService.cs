using CookieShop.App.DTOs.User;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Constants;
using Mapster;

namespace CookieShop.App.Services;

public class UserService(IUserRepository userRepository)
{
    public async Task<UserResponse> GetById(string id)
    {
        var user = await userRepository.GetById(id);
        return user == null ? throw new NotFoundException("User not found") : user.Adapt<UserResponse>();
    }

    public async Task<UserResponse> GetByPhoneNumber(string phoneNumber)
    {
        var user = await userRepository.GetByPhoneNumber(phoneNumber);
        return user == null ? throw new NotFoundException("User not found") : user.Adapt<UserResponse>();
    }

    public async Task<UserResponse> Create(CreateUserRequest request)
    {
        var user = await userRepository.Create(request);
        var assignRole = await userRepository.AssignRole(user, UserRole.Customer);
        return assignRole == UserRole.Customer
            ? user.Adapt<UserResponse>()
            : throw new ConflictException("Role cannot be assigned");
    }

    public async Task<UserResponse> Update(Guid? id, UpdateUserRequest request)
    {
        if (id == null) throw new UnauthorizedAccessException("User id not found");
        if (id.ToString() != request.Id) throw new UnauthorizedAccessException("User id does not match");
        var user = await userRepository.Update(request);
        return user.Adapt<UserResponse>();
    }

    public async Task<UserResponse> Me(Guid id)
    {
        var result = await userRepository.GetById(id.ToString());
        return result == null ? throw new NotFoundException("User not found") : result.Adapt<UserResponse>();
    }
}