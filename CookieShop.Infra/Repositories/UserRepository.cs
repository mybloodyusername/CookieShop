using CookieShop.App.Interfaces;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;

namespace CookieShop.Infra.Repositories;

public class UserRepository : IUserRepository
{
    public Task<ApplicationUser> GetUserByIdAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser> GetUserByUsername(string username)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser> CreateUser(ApplicationUser applicationUser)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser> UpdateUser(ApplicationUser applicationUser)
    {
        throw new NotImplementedException();
    }
}