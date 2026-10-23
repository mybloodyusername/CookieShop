using System.Text;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Constants;
using System.Security.Claims;
using CookieShop.Infra.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CookieShop.Infra.Extensions;

public static class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        public void AddCookieShopDbContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<CookieShopDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        public void AddIdentityDbContext(IConfiguration configuration)
        {
            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 12;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.AllowedForNewUsers = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<CookieShopDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();
        }

        public void AddJwtAuthentication(IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secret = jwtSettings["SecretKey"];
            if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32 || secret.Contains("YourSuperSecret"))
                throw new InvalidOperationException("Set JwtSettings:SecretKey to a random secret of at least 32 bytes via environment variables or user secrets.");
            if (string.IsNullOrWhiteSpace(jwtSettings["Issuer"]) || string.IsNullOrWhiteSpace(jwtSettings["Audience"]))
                throw new InvalidOperationException("JWT issuer and audience are required.");
            var minutes = jwtSettings.GetValue<int>("ExpirationMinutes");
            if (minutes is < 5 or > 120) throw new InvalidOperationException("JWT expiration must be between 5 and 120 minutes.");
            services.AddAuthorization(options => options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build());

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        ValidIssuer = jwtSettings.GetSection("Issuer").Value!,
                        ValidAudience = jwtSettings.GetSection("Audience").Value!,
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(secret))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var manager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                            var user = userId == null ? null : await manager.FindByIdAsync(userId);
                            if (user == null || await manager.IsLockedOutAsync(user) ||
                                context.Principal?.FindFirstValue("security_stamp") != user.SecurityStamp)
                            {
                                context.Fail("Session is no longer valid.");
                                return;
                            }
                            // Do not trust stale role claims after an owner's role change.
                            var identity = (ClaimsIdentity)context.Principal!.Identity!;
                            foreach (var role in identity.FindAll(ClaimTypes.Role).ToList()) identity.RemoveClaim(role);
                            identity.AddClaims((await manager.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));
                        },
                        OnMessageReceived = context =>
                        {
                            // read token from HttpOnly cookie
                            if (!context.Request.Headers.ContainsKey("Authorization") && context.Request.Cookies.TryGetValue("CookieShop.Token", out var token))
                                context.Token = token;
                            return Task.CompletedTask;
                        }
                    };
                });
        }

        public void AddCorsPolicies(IConfiguration configuration)
        {
            var corsSettings = configuration.GetSection("CorsSettings");

            services.AddCors(options =>
            {
                options.AddPolicy("DevelopmentPolicy", policy =>
                {
                    policy.WithOrigins(corsSettings.GetSection("DevelopmentOrigin").Value!)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });

                options.AddPolicy("ProductionPolicy", policy =>
                {
                    policy.WithOrigins(corsSettings.GetSection("ProductionOrigin").Value!)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
        }
    }
}