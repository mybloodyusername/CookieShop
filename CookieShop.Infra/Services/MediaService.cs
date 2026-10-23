using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using CookieShop.App.DTOs.Common;
using CookieShop.App.DTOs.Media;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces;
using CookieShop.App.Services;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace CookieShop.Infra.Services;

public sealed class MediaService(CookieShopDbContext context, IEnumerable<IImageStorage> storageAdapters, IConfiguration configuration, MediaUrlProvider urls)
{
    public const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly SemaphoreSlim ProcessingSlots = new(2, 2);
    private IImageStorage Storage(string provider) => storageAdapters.Single(x => x.Provider == provider);
    private ImageResponse Response(MediaImage image) => new(image.Id, urls.Get(image.Id), image.Alt,
        image.Album, image.FileName, image.Width, image.Height, image.ByteLength, image.CreatedAt);

    public async Task<ImageResponse> UploadAsync(Stream input, string alt, string? album, CancellationToken ct)
    {
        ValidateMetadata(alt, album);
        await ProcessingSlots.WaitAsync(ct);
        try
        {
            // Do not trust extension, supplied MIME type, or Content-Length.
            using var source = new MemoryStream();
            var buffer = new byte[81920];
            int count;
            while ((count = await input.ReadAsync(buffer, ct)) > 0)
            {
                if (source.Length + count > MaxUploadBytes) throw new ValidationException("Image exceeds 10 MiB.");
                await source.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            var processed = WebpImageProcessor.Process(source.ToArray());
            var bytes = processed.Bytes;
            var provider = configuration["Media:Provider"] ?? "Postgres";
            var storage = Storage(provider);
            var record = new MediaImage
            {
                Alt = alt.Trim(), Album = album?.Trim(), Width = processed.Width, Height = processed.Height,
                StorageProvider = provider, ByteLength = bytes.Length,
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
            };
            record.StorageKey = record.Id.ToString("D");
            record.FileName = $"{record.Id:D}.webp";
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            context.MediaImages.Add(record);
            await context.SaveChangesAsync(ct);
            try
            {
                await storage.PutAsync(record.StorageKey, bytes, ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                if (provider != "Postgres") await storage.DeleteAsync(record.StorageKey, CancellationToken.None);
                throw;
            }
            return Response(record);
        }
        finally { ProcessingSlots.Release(); }
    }

    public async Task<PagedResult<ImageResponse>> ListAsync(string? album, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = context.MediaImages.AsNoTracking();
        if (album != null) query = query.Where(x => x.Album == album);
        var total = await query.CountAsync(ct);
        var images = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(images.Select(Response).ToList(), total, page, pageSize);
    }

    public async Task<ImageResponse> UpdateAsync(Guid id, string alt, string? album, CancellationToken ct)
    {
        ValidateMetadata(alt, album);
        var image = await context.MediaImages.FindAsync([id], ct) ?? throw new NotFoundException("Image not found.");
        image.Alt = alt.Trim(); image.Album = album?.Trim();
        await context.SaveChangesAsync(ct);
        return Response(image);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var image = await context.MediaImages.FindAsync([id], ct) ?? throw new NotFoundException("Image not found.");
        if (await context.ProductImages.AnyAsync(x => x.ImageId == id, ct))
            throw new ConflictException("Detach this image from its products before deleting it.");
        // FK restrictions also protect against a concurrent attachment.
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        context.MediaImages.Remove(image);
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new ConflictException("Image is still attached to a product."); }
        await Storage(image.StorageProvider).DeleteAsync(image.StorageKey, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<MediaImage?> FindAsync(Guid id, CancellationToken ct) =>
        await context.MediaImages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<byte[]?> ReadAsync(MediaImage image, CancellationToken ct) => Storage(image.StorageProvider).GetAsync(image.StorageKey, ct);

    public async Task AttachAsync(Guid productId, Guid imageId, string? alt, int sortOrder, CancellationToken ct)
    {
        if (alt?.Length > 500 || sortOrder < 0) throw new ValidationException("Invalid alt or sort order.");
        if (!await context.Products.AnyAsync(x => x.Id == productId, ct)) throw new NotFoundException("Product not found.");
        if (!await context.MediaImages.AnyAsync(x => x.Id == imageId, ct)) throw new NotFoundException("Image not found.");
        var link = await context.ProductImages.FindAsync([productId, imageId], ct);
        if (link == null) context.ProductImages.Add(link = new ProductImage { ProductId = productId, ImageId = imageId });
        link.Alt = alt?.Trim(); link.SortOrder = sortOrder;
        try { await context.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new ConflictException("Image attachment changed; reload and retry."); }
    }
    public async Task DetachAsync(Guid productId, Guid imageId, CancellationToken ct)
    {
        if (await context.ProductImages.Where(x => x.ProductId == productId && x.ImageId == imageId).ExecuteDeleteAsync(ct) == 0)
            throw new NotFoundException("Product image not found.");
    }
    private static void ValidateMetadata(string alt, string? album)
    {
        if (alt == null || alt.Length > 500 || album?.Length > 100)
            throw new ValidationException("Alt must be at most 500 characters and album at most 100.");
    }
}
