namespace CookieShop.Domain.Entities;

public class ImageBlob
{
    public Guid Id { get; set; }
    public byte[] Data { get; set; } = [];
}
