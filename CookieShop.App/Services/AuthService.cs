using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
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
        if (user == null || user.IsDeleted) throw new InvalidCredentialException("Invalid phone number or password.");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            throw new InvalidCredentialException("Invalid phone number or password.");

        var roles = await userManager.GetRolesAsync(user);
        await IssueSession(user, roles);
        return ToUserResponse(user, roles);
    }

    public async Task<UserResponse> Refresh()
    {
        var context = httpContextAccessor.HttpContext!;
        if (!context.Request.Cookies.TryGetValue("CookieShop.Refresh", out var refreshToken) ||
            string.IsNullOrWhiteSpace(refreshToken))
        {
            ClearSessionCookies(context);
            throw new InvalidCredentialException("Session expired. Please sign in again.");
        }

        if (!TryParseRefreshToken(refreshToken, out var userId, out var sessionId))
        {
            ClearSessionCookies(context);
            throw new InvalidCredentialException("Session expired. Please sign in again.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        var tokenName = RefreshTokenName(sessionId);
        var storedToken = user == null
            ? null
            : await userManager.GetAuthenticationTokenAsync(user, "CookieShop", tokenName);
        var storedParts = storedToken?.Split(':', 3);
        var validStoredToken = user != null && storedParts is { Length: 3 } &&
            long.TryParse(storedParts[0], out var expiryUnixSeconds) &&
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expiryUnixSeconds &&
            string.Equals(storedParts[1], user.SecurityStamp, StringComparison.Ordinal) &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(storedParts[2]),
                Encoding.ASCII.GetBytes(HashRefreshToken(refreshToken)));
        if (!validStoredToken || user == null || user.IsDeleted ||
            await userManager.IsLockedOutAsync(user))
        {
            if (user != null)
                await userManager.RemoveAuthenticationTokenAsync(user, "CookieShop", tokenName);
            ClearSessionCookies(context);
            throw new InvalidCredentialException("Session expired. Please sign in again.");
        }

        var roles = await userManager.GetRolesAsync(user);
        await IssueSession(user, roles, sessionId);
        return ToUserResponse(user, roles);
    }

    private async Task IssueSession(ApplicationUser user, IList<string> roles, Guid? existingSessionId = null)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var refreshDays = jwtSettings.GetValue<int?>("RefreshTokenExpirationDays") ?? 30;
        if (refreshDays is < 1 or > 90)
            throw new InvalidOperationException("JwtSettings:RefreshTokenExpirationDays must be between 1 and 90.");

        var now = DateTimeOffset.UtcNow;
        var sessionId = existingSessionId ?? Guid.NewGuid();
        var tokenName = RefreshTokenName(sessionId);
        var refreshToken = $"{user.Id:N}.{sessionId:N}.{Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64))}";
        var refreshExpiresAt = now.AddDays(refreshDays);
        var storedToken = $"{refreshExpiresAt.ToUnixTimeSeconds()}:{user.SecurityStamp}:{HashRefreshToken(refreshToken)}";
        EnsureSuccess(await userManager.SetAuthenticationTokenAsync(user, "CookieShop", tokenName, storedToken));

        var context = httpContextAccessor.HttpContext!;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            Secure = !environment.IsDevelopment() || context.Request.IsHttps
        };
        var accessMinutes = jwtSettings.GetValue<int>("ExpirationMinutes");
        context.Response.Cookies.Append("CookieShop.Token", GenerateJwtToken(user, roles),
            new CookieOptions(cookieOptions) { Expires = now.AddMinutes(accessMinutes) });
        context.Response.Cookies.Append("CookieShop.Refresh", refreshToken,
            new CookieOptions(cookieOptions) { Expires = refreshExpiresAt });
    }

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool TryParseRefreshToken(string token, out Guid userId, out Guid sessionId)
    {
        userId = Guid.Empty;
        sessionId = Guid.Empty;
        var userSeparator = token.IndexOf('.');
        var sessionSeparator = userSeparator < 0 ? -1 : token.IndexOf('.', userSeparator + 1);
        return userSeparator == 32 && sessionSeparator - userSeparator - 1 == 32 &&
            Guid.TryParseExact(token[..userSeparator], "N", out userId) &&
            Guid.TryParseExact(token[(userSeparator + 1)..sessionSeparator], "N", out sessionId);
    }

    private static string RefreshTokenName(Guid sessionId) => $"Refresh:{sessionId:N}";

    private static UserResponse ToUserResponse(ApplicationUser user, IList<string> roles)
    {
        var response = user.Adapt<UserResponse>();
        response.Role = roles.Contains(UserRole.Admin) ? UserRole.Admin : roles.FirstOrDefault();
        return response;
    }

    private void ClearSessionCookies(HttpContext context)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Secure = !environment.IsDevelopment() || context.Request.IsHttps
        };
        context.Response.Cookies.Delete("CookieShop.Token", options);
        context.Response.Cookies.Delete("CookieShop.Refresh", options);
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
        var context = httpContextAccessor.HttpContext!;
        if (user != null)
        {
            var refreshCookie = context.Request.Cookies["CookieShop.Refresh"];
            if (!string.IsNullOrWhiteSpace(refreshCookie) &&
                TryParseRefreshToken(refreshCookie, out _, out var sessionId))
                EnsureSuccess(await userManager.RemoveAuthenticationTokenAsync(
                    user, "CookieShop", RefreshTokenName(sessionId)));
            EnsureSuccess(await userManager.UpdateSecurityStampAsync(user));
        }
        ClearSessionCookies(context);
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
