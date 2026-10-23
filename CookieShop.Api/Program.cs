using System.Threading.RateLimiting;
using CookieShop.Api.Security;
using Microsoft.AspNetCore.DataProtection;
using Amazon.S3;
using CookieShop.App.Interfaces;
using CookieShop.Infra.Storage;
using CookieShop.Infra.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CookieShop.Api.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.App.Services;
using CookieShop.Infra.Extensions;
using CookieShop.Infra.Repositories;
using CookieShop.Infra.Data;
using Scalar.AspNetCore;

var resetDemoData = args.Contains("--reset-demo-data", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(x => x != "--reset-demo-data").ToArray());
if (resetDemoData && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("The sample-data reset command is available only in Development.");

builder.Logging.ClearProviders().AddConsole();

builder.Services.AddControllers(options => options.Filters.Add(new TypeFilterAttribute(typeof(CsrfValidationFilter))));
var protection = builder.Services.AddDataProtection().SetApplicationName("CookieShop");
if (builder.Configuration["DataProtection:KeyPath"] is { Length: > 0 } keyPath)
    protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CookieShop.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<IImageStorage, PostgresImageStorage>();
var mediaProvider = builder.Configuration["Media:Provider"] ?? "Postgres";
if (mediaProvider is not ("Postgres" or "S3")) throw new InvalidOperationException("Media:Provider must be Postgres or S3.");
var s3Endpoint = builder.Configuration["Media:S3:Endpoint"];
if (!string.IsNullOrWhiteSpace(s3Endpoint))
{
    if (!Uri.TryCreate(s3Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https")
        throw new InvalidOperationException("Media:S3:Endpoint must be an HTTPS URL.");
    foreach (var key in new[] { "Bucket", "AccessKey", "SecretKey", "Region" })
        if (string.IsNullOrWhiteSpace(builder.Configuration[$"Media:S3:{key}"]))
            throw new InvalidOperationException($"Media:S3:{key} is required.");
    builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
        builder.Configuration["Media:S3:AccessKey"], builder.Configuration["Media:S3:SecretKey"],
        new AmazonS3Config { ServiceURL = s3Endpoint, ForcePathStyle = true,
            AuthenticationRegion = builder.Configuration["Media:S3:Region"],
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED }));
    builder.Services.AddScoped<IImageStorage, S3ImageStorage>();
}
else if (mediaProvider == "S3") throw new InvalidOperationException("Media:S3:Endpoint is required.");
builder.Services.AddSingleton<MediaUrlProvider>();
builder.Services.AddScoped<MediaService>();
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
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

if (resetDemoData)
{
    await using (var scope = app.Services.CreateAsyncScope())
        await PersianDemoData.ResetAsync(scope.ServiceProvider);
    await app.DisposeAsync();
    return;
}

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors("ProductionPolicy");
}

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program;
