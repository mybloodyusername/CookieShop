using System.ComponentModel.DataAnnotations;
using CookieShop.App.DTOs.User;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Constants;
using CookieShop.Infra.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class UserRepository(
    CookieShopDbContext context,
    UserManager<ApplicationUser> userManager) : IUserRepository
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetRoles(IReadOnlyCollection<Guid> userIds)
    {
        var memberships = await (from membership in context.UserRoles
            join role in context.Roles on membership.RoleId equals role.Id
            where userIds.Contains(membership.UserId)
            select new { membership.UserId, role.Name }).ToListAsync();
        return memberships.GroupBy(x => x.UserId).ToDictionary(x => x.Key,
            x => x.Any(r => r.Name == UserRole.Admin) ? UserRole.Admin : x.First().Name!);
    }

    public async Task<ApplicationUser?> GetById(Guid userId)
    {
        return await userManager.FindByIdAsync(userId.ToString());
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

        await using var transaction = await context.Database.BeginTransactionAsync();
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
        if (userResult.Succeeded)
        {
            var roleResult = await userManager.AddToRoleAsync(newUser, UserRole.Customer);
            if (!roleResult.Succeeded) throw new ValidationException(string.Join("; ", roleResult.Errors.Select(x => x.Description)));
            await transaction.CommitAsync();
            return newUser;
        }
        var errors = string.Join("; ", userResult.Errors.Select(e => e.Description));
        throw new ValidationException(errors);
    }

    public async Task<ApplicationUser> Update(UpdateUserRequest request)
    {
        var existingUser = await userManager.FindByIdAsync(request.Id.ToString());
        if (existingUser == null) throw new NotFoundException("User not found.");

        if (await context.Users.AnyAsync(x => x.Id != request.Id && x.PhoneNumber == request.PhoneNumber))
            throw new DuplicateException("Phone number already exists.");
        var existingEmail = await userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null && existingEmail.Id != request.Id)
            throw new DuplicateException("Email already exists.");
        existingUser.PhoneNumberConfirmed = existingUser.PhoneNumber == request.PhoneNumber && existingUser.PhoneNumberConfirmed;
        existingUser.EmailConfirmed = existingUser.Email == request.Email && existingUser.EmailConfirmed;
        existingUser.UserName = request.PhoneNumber;
        existingUser.PhoneNumber = request.PhoneNumber;
        existingUser.Email = request.Email;
        existingUser.FirstName = request.FirstName;
        existingUser.LastName = request.LastName;
        existingUser.UpdatedAt = DateTime.UtcNow;

        existingUser.SecurityStamp = Guid.NewGuid().ToString();
        var result = await userManager.UpdateAsync(existingUser);
        if (result.Succeeded) return existingUser;
        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
        throw new ValidationException(errors);
    }

    public async Task<string> AssignRole(ApplicationUser applicationUser, string userRole)
    {
        if (!UserRole.All.Contains(userRole)) throw new ValidationException("Invalid role.");
        var existing = await userManager.GetRolesAsync(applicationUser);
        if (existing.Any(x => x != userRole)) throw new ConflictException("Administrator and customer roles cannot be combined.");
        var roleResult = await userManager.AddToRoleAsync(applicationUser, userRole);
        if (roleResult.Succeeded) return userRole;
        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
        throw new ValidationException(errors);
    }
}
