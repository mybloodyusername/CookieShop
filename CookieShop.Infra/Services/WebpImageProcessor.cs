using System.ComponentModel.DataAnnotations;
using SkiaSharp;
namespace CookieShop.Infra.Services;

public static class WebpImageProcessor
{
    public record Result(byte[] Bytes, int Width, int Height);
    public static Result Process(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.LongLength > MediaService.MaxUploadBytes)
            throw new ValidationException("Image must contain between 1 byte and 10 MiB.");
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec == null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            throw new ValidationException("Only valid JPEG, PNG and WebP images are accepted.");
        var w = codec.Info.Width; var h = codec.Info.Height;
        if (w <= 0 || h <= 0 || (long)w * h > 40_000_000 || w > 16384 || h > 16384)
            throw new ValidationException("Image exceeds 40 megapixels or 16384 pixels per side.");
        using var decoded = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
            throw new ValidationException("Image data is incomplete or corrupt.");
        var origin = codec.EncodedOrigin;
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var orientedW = swap ? h : w; var orientedH = swap ? w : h;
        var scale = Math.Min(1d, 2048d / Math.Max(orientedW, orientedH));
        var width = Math.Max(1, (int)Math.Round(orientedW * scale));
        var height = Math.Max(1, (int)Math.Round(orientedH * scale));
        using var output = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(output))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)width / orientedW, (float)height / orientedH);
            var matrix = origin switch
            {
                SKEncodedOrigin.TopRight => Matrix(-1, 0, w, 0, 1, 0),
                SKEncodedOrigin.BottomRight => Matrix(-1, 0, w, 0, -1, h),
                SKEncodedOrigin.BottomLeft => Matrix(1, 0, 0, 0, -1, h),
                SKEncodedOrigin.LeftTop => Matrix(0, 1, 0, 1, 0, 0),
                SKEncodedOrigin.RightTop => Matrix(0, -1, h, 1, 0, 0),
                SKEncodedOrigin.RightBottom => Matrix(0, -1, h, -1, 0, w),
                SKEncodedOrigin.LeftBottom => Matrix(0, 1, 0, -1, 0, w),
                _ => SKMatrix.Identity
            };
            canvas.Concat(matrix);
            canvas.DrawBitmap(decoded, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }
        // A fresh bitmap excludes source EXIF, GPS, profiles and embedded metadata.
        using var image = SKImage.FromBitmap(output);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 80)
            ?? throw new ValidationException("Image cannot be encoded as WebP.");
        return new(encoded.ToArray(), width, height);
    }
    private static SKMatrix Matrix(float sx, float kx, float tx, float ky, float sy, float ty) => new()
    { ScaleX = sx, SkewX = kx, TransX = tx, SkewY = ky, ScaleY = sy, TransY = ty, Persp2 = 1 };
}
