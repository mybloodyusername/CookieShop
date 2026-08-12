using System.Text;
using CookieShop.Domain.Entities;
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
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                    options.User.RequireUniqueEmail = false;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<CookieShopDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders();
        }

        public void AddJwtAuthentication(IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");

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
                        ValidIssuer = jwtSettings.GetSection("Issuer").Value!,
                        ValidAudience = jwtSettings.GetSection("Audience").Value!,
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtSettings.GetSection("SecretKey").Value!))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // read token from HttpOnly cookie
                            if (context.Request.Cookies.TryGetValue("CookieShop.Token", out var token))
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