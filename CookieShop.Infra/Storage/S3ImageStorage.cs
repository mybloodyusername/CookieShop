using Amazon.S3;
using Amazon.S3.Model;
using CookieShop.App.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
namespace CookieShop.Infra.Storage;

// Liara and Arvan can use the same adapter with their own endpoint and credentials.
public sealed class S3ImageStorage(IAmazonS3 client, IConfiguration configuration) : IImageStorage
{
    public string Provider => "S3";
    private string Bucket => configuration["Media:S3:Bucket"] ?? throw new InvalidOperationException("Media:S3:Bucket is required.");
    private static string ObjectKey(string key) => $"images/{Guid.Parse(key):D}.webp";
    public async Task PutAsync(string key, byte[] bytes, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket, Key = ObjectKey(key), InputStream = stream,
            ContentType = "image/webp"
        }, cancellationToken);
    }
    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetObjectAsync(Bucket, ObjectKey(key), cancellationToken);
            using var stream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(stream, cancellationToken);
            return stream.ToArray();
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound) { return null; }
    }
    public async Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        await client.DeleteObjectAsync(Bucket, ObjectKey(key), cancellationToken);
}
