using CookieShop.App.Interfaces;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
namespace CookieShop.Infra.Storage;

public sealed class PostgresImageStorage(CookieShopDbContext context) : IImageStorage
{
    public string Provider => "Postgres";
    public async Task PutAsync(string key, byte[] bytes, CancellationToken cancellationToken)
    {
        context.ImageBlobs.Add(new ImageBlob { Id = Guid.Parse(key), Data = bytes });
        await context.SaveChangesAsync(cancellationToken);
    }
    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken) => context.ImageBlobs
        .Where(x => x.Id == Guid.Parse(key)).Select(x => x.Data).FirstOrDefaultAsync(cancellationToken);
    public Task DeleteAsync(string key, CancellationToken cancellationToken) => context.ImageBlobs
        .Where(x => x.Id == Guid.Parse(key)).ExecuteDeleteAsync(cancellationToken);
}
