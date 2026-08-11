using CookieShop.App.DTOs.Auth;
using CookieShop.App.DTOs.User;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace CookieShop.App.Services;

public class AuthService(
    IUserRepository userRepository,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager)
{
    public async Task<UserResponse> Login(LoginRequest request)
    {
        var user = await userRepository.GetByPhoneNumber(request.PhoneNumber);
        if (user == null) throw new NotFoundException("User not found.");
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (result.Succeeded) user.Adapt<UserResponse>();
        throw new UnauthorizedAccessException("Invalid username or password.");
    }
    
    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
        var newUser = await userRepository.Create(new CreateUserRequest
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Password = request.Password,
        });
        return new RegisterResponse{
            IsSuccess = true,
        };
    }
}