using CookieShop.Api.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.App.Services;
using CookieShop.Infra.Extensions;
using CookieShop.Infra.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddConsole();
builder.Services.AddLogging(options => options.SetMinimumLevel(LogLevel.Trace).AddConsole());

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCookieShopDbContext(builder.Configuration);
builder.Services.AddIdentityDbContext(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicies(builder.Configuration);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();


var app = builder.Build();

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors("ProductionPolicy");
}


app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();