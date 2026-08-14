using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CookieShop.App.DTOs.Auth;
using CookieShop.App.DTOs.User;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CookieShop.App.Services;

public class AuthService(
    IUserRepository userRepository,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
{
    public async Task<UserResponse> Login(LoginRequest request)
    {
        var user = await userRepository.GetByPhoneNumber(request.PhoneNumber);
        if (user == null) throw new NotFoundException("User not found.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var roles = await userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);
        var jwtSettings = configuration.GetSection("JwtSettings");
        var expirationDays = jwtSettings.GetSection("ExpirationDays").Get<int>();

        var context = httpContextAccessor.HttpContext!;
        context.Response.Cookies.Append("CookieShop.Token", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(expirationDays)
        });

        return user.Adapt<UserResponse>();
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.PhoneNumber!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(jwtSettings.GetSection("ExpirationDays").Get<int>()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
        var newUser = await userRepository.Create(new CreateUserRequest
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Password = request.Password,
        });
        var assignRole = await userRepository.AssignRole(newUser, UserRole.Customer);
        if (assignRole != UserRole.Customer)
            throw new ConflictException("Role cannot be assigned");
        return new RegisterResponse
        {
            IsSuccess = true,
        };
    }
}