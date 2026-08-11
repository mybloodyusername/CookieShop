using CookieShop.App.DTOs.User;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface IUserRepository
{
    public Task<ApplicationUser?> GetById(string userId);
    public Task<ApplicationUser?> GetByPhoneNumber(string username);
    public Task<ApplicationUser> Create(CreateUserRequest request);
    public Task<ApplicationUser> Update(UpdateUserRequest request);
    public Task<string> AssignRole(ApplicationUser applicationUser, string userRole);
}