using System.ComponentModel.DataAnnotations;
using CookieShop.Domain.Constants;
using CookieShop.Infra.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CookieShop.Api.Controllers;

[ApiController, Route("api/images"), Authorize(Roles = UserRole.Admin)]
public class ImagesController(MediaService media) : ControllerBase
{
    [AllowAnonymous, HttpGet("{id:guid}.webp")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var image = await media.FindAsync(id, ct);
        if (image == null) return NotFound();
        var etag = new EntityTagHeaderValue($"\"{image.Sha256}\"");
        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        Response.Headers.ETag = etag.ToString();
        if (Request.GetTypedHeaders().IfNoneMatch?.Any(x => x.Tag == "*" || x.Compare(etag, useStrongComparison: false)) == true)
            return StatusCode(StatusCodes.Status304NotModified);
        var bytes = await media.ReadAsync(image, ct);
        return bytes == null ? NotFound() : File(bytes, "image/webp", image.CreatedAt, etag);
    }

    [HttpGet]
    public async Task<IActionResult> List(string? album, CancellationToken ct, int page = 1, int pageSize = 30) =>
        Ok(await media.ListAsync(album, page, pageSize, ct));

    [HttpPost, RequestSizeLimit(MediaService.MaxUploadBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = MediaService.MaxUploadBytes + 65536)]
    public async Task<IActionResult> Upload([FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        if (request.File.Length == 0 || request.File.Length > MediaService.MaxUploadBytes)
            return BadRequest(new { error = "Image must contain between 1 byte and 10 MiB." });
        await using var stream = request.File.OpenReadStream();
        var result = await media.UploadAsync(stream, request.Alt, request.Album, ct);
        return Created(result.Url, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ImageMetadataRequest request, CancellationToken ct) =>
        Ok(await media.UpdateAsync(id, request.Alt, request.Album, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    { await media.DeleteAsync(id, ct); return NoContent(); }

    [HttpPut("products/{productId:guid}/{imageId:guid}")]
    public async Task<IActionResult> Attach(Guid productId, Guid imageId, ProductImageRequest request, CancellationToken ct)
    { await media.AttachAsync(productId, imageId, request.Alt, request.SortOrder, ct); return NoContent(); }

    [HttpDelete("products/{productId:guid}/{imageId:guid}")]
    public async Task<IActionResult> Detach(Guid productId, Guid imageId, CancellationToken ct)
    { await media.DetachAsync(productId, imageId, ct); return NoContent(); }
}
public class ImageUploadRequest
{
    [Required] public required IFormFile File { get; set; }
    [MaxLength(500)] public string Alt { get; set; } = "";
    [MaxLength(100)] public string? Album { get; set; }
}
public record ImageMetadataRequest([MaxLength(500)] string Alt, [MaxLength(100)] string? Album);
public record ProductImageRequest([MaxLength(500)] string? Alt, [Range(0, int.MaxValue)] int SortOrder);
