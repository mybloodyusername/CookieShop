using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface IUserRepository
{
    public Task<ApplicationUser> GetUserByIdAsync(string userId);
    public Task<ApplicationUser> GetUserByUsername(string username);
    public Task<ApplicationUser> CreateUser(ApplicationUser applicationUser);
    public Task<ApplicationUser> UpdateUser(ApplicationUser applicationUser);
}