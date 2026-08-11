using System.ComponentModel.DataAnnotations;
using CookieShop.App.DTOs.User;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class UserRepository(
    CookieShopDbContext context,
    UserManager<ApplicationUser> userManager) : IUserRepository
{
    public async Task<ApplicationUser?> GetById(string userId)
    {
        return await userManager.FindByIdAsync(userId);
    }

    public async Task<ApplicationUser?> GetByPhoneNumber(string phoneNumber)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<ApplicationUser> Create(CreateUserRequest request)
    {
        var existingUserByEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingUserByEmail != null) throw new DuplicateException("Email already exists.");

        var existingUserByPhoneNumber =
            await context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
        if (existingUserByPhoneNumber != null) throw new DuplicateException("PhoneNumber already exists.");

        var newUser = new ApplicationUser
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            UserName = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var userResult = await userManager.CreateAsync(newUser, request.Password);
        if (userResult.Succeeded) return newUser;
        var errors = string.Join("; ", userResult.Errors.Select(e => e.Description));
        throw new ValidationException(errors);
    }

    public async Task<ApplicationUser> Update(UpdateUserRequest request)
    {
        var existingUser = await userManager.FindByIdAsync(request.Id);
        if (existingUser == null) throw new NotFoundException("User not found.");

        existingUser.PhoneNumber = request.PhoneNumber;
        existingUser.Email = request.Email;
        existingUser.FirstName = request.FirstName;
        existingUser.LastName = request.LastName;
        existingUser.UpdatedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(existingUser);
        if (result.Succeeded) return existingUser;
        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
        throw new ValidationException(errors);
    }

    public async Task<string> AssignRole(ApplicationUser applicationUser, string userRole)
    {
        var roleResult = await userManager.AddToRoleAsync(applicationUser, userRole);
        if (roleResult.Succeeded) return userRole;
        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
        throw new ValidationException(errors);
    }
}