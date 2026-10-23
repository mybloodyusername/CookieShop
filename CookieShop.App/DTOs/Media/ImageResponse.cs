namespace CookieShop.App.DTOs.Media;

public record ImageResponse(Guid Id, string Url, string Alt, string? Album, string FileName,
    int Width, int Height, long ByteLength, DateTimeOffset CreatedAt);
public record ProductImageResponse(Guid ImageId, string Url, string Alt, int SortOrder);
