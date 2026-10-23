using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
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
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace CookieShop.App.Services;

public class AuthService(
    IUserRepository userRepository,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    public async Task<UserResponse> Login(LoginRequest request)
    {
        var user = await userRepository.GetByPhoneNumber(request.PhoneNumber);
        if (user == null) throw new InvalidCredentialException("Invalid phone number or password.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            throw new InvalidCredentialException("Invalid phone number or password.");

        var roles = await userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);
        var jwtSettings = configuration.GetSection("JwtSettings");
        var expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes");

        var context = httpContextAccessor.HttpContext!;
        context.Response.Cookies.Append("CookieShop.Token", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            Secure = !environment.IsDevelopment() || context.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        });

        var response = user.Adapt<UserResponse>();
        response.Role = roles.FirstOrDefault();
        return response;
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
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("security_stamp", user.SecurityStamp ?? "")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.GetValue<int>("ExpirationMinutes")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task Logout(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null) EnsureSuccess(await userManager.UpdateSecurityStampAsync(user));
        httpContextAccessor.HttpContext!.Response.Cookies.Delete("CookieShop.Token", new CookieOptions { Path = "/" });
    }

    public async Task ChangePassword(Guid userId, ChangePasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new InvalidCredentialException("Invalid session.");
        EnsureSuccess(await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));
        await Logout(userId);
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded) throw new ValidationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }

    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
        await userRepository.Create(new CreateUserRequest
        {
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Password = request.Password,
        });
        return new RegisterResponse
        {
            IsSuccess = true,
        };
    }
}