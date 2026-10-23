namespace CookieShop.App.Interfaces;

public interface IImageStorage
{
    string Provider { get; }
    Task PutAsync(string key, byte[] bytes, CancellationToken cancellationToken);
    Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
