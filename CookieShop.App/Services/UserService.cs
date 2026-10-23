using CookieShop.App.DTOs.User;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Constants;
using Mapster;
using CookieShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CookieShop.App.DTOs.Common;

namespace CookieShop.App.Services;

public class UserService(IUserRepository userRepository, UserManager<ApplicationUser> userManager)
{
    public async Task<PagedResult<UserResponse>> GetAll(int page, int pageSize)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = userManager.Users.AsNoTracking();
        var total = await query.CountAsync();
        var users = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new(users.Adapt<IReadOnlyCollection<UserResponse>>(), total, page, pageSize);
    }

    public async Task<UserResponse> GetById(Guid id)
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
        return user.Adapt<UserResponse>() with { Role = UserRole.Customer };
    }

    public async Task<UserResponse> Update(Guid? id, UpdateUserRequest request)
    {
        if (id == null) throw new UnauthorizedAccessException("User id not found");
        if (!id.Equals(request.Id)) throw new UnauthorizedAccessException("User id does not match");
        var user = await userRepository.Update(request);
        return user.Adapt<UserResponse>();
    }

    public async Task<UserResponse> Me(Guid id)
    {
        var result = await userRepository.GetById(id);
        return result == null ? throw new NotFoundException("User not found") : result.Adapt<UserResponse>();
    }
}