using CookieShop.App.DTOs.User;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface IUserRepository
{
    public Task<ApplicationUser?> GetUserByIdAsync(string userId);
    public Task<ApplicationUser?> GetUserByPhoneNumber(string username);
    public Task<ApplicationUser> CreateUser(CreateUserRequest request);
    public Task<ApplicationUser> UpdateUser(UpdateUserRequest request);
    public Task<string> AssignRole(ApplicationUser applicationUser, string userRole);
}