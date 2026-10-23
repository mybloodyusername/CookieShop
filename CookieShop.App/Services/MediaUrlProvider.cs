using Microsoft.Extensions.Configuration;

namespace CookieShop.App.Services;

public sealed class MediaUrlProvider
{
    private readonly string baseUrl;
    public MediaUrlProvider(IConfiguration configuration)
    {
        baseUrl = (configuration["Media:PublicBaseUrl"] ?? "").TrimEnd('/');
        if (baseUrl.Length > 0 && (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http") || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0))
            throw new InvalidOperationException("Media:PublicBaseUrl must be an HTTP(S) base URL without credentials, query or fragment.");
    }
    public string Get(Guid id) => $"{baseUrl}/api/images/{id:D}.webp";
}
