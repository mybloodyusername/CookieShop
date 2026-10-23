using SkiaSharp;

namespace CookieShop.Infra.Data;

// Self-contained sample illustrations, not photographs of actual inventory.
// They use the same WebP byte storage and public URLs as uploaded product photos.
internal static class SampleProductArtwork
{
    internal static byte[] Create(string flavor, int productIndex, int view)
    {
        using var bitmap = new SKBitmap(720, 720);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColor.Parse(view == 0 ? "#f6efe5" : "#eee5d8"));
        var box = flavor.EndsWith("box", StringComparison.Ordinal);
        if (box)
        {
            DrawRect(canvas, new SKRect(76, 88, 660, 658), 24, "#d3b695");
            DrawRect(canvas, new SKRect(60, 70, 644, 640), 24, "#bb875d");
            DrawRect(canvas, new SKRect(83, 93, 621, 617), 12, "#f5e5cd");
            for (var i = 0; i < 6; i++)
                DrawCookie(canvas, 190 + i % 2 * 260, 183 + i / 2 * 167, 69,
                    productIndex + i, view, flavor == "chocolate-box" ? "dark-chocolate" : i % 2 == 0 ? "pistachio" : "chocolate-chip");
            DrawRect(canvas, new SKRect(340, 70, 365, 640), 2, "#747755");
        }
        else
        {
            using var shadow = new SKPaint { IsAntialias = true, Color = new SKColor(80, 55, 30, 22) };
            canvas.DrawOval(105, 133, 635, 663, shadow);
            using var plate = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#fffdf8") };
            canvas.DrawCircle(356, 370, 266, plate);
            using var rim = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2, Color = SKColor.Parse("#e7dfd2") };
            canvas.DrawCircle(356, 370, 244, rim);
            if (view == 0)
            {
                DrawCookie(canvas, 270, 292, 136, productIndex, view, flavor);
                DrawCookie(canvas, 438, 430, 143, productIndex + 11, view, flavor);
            }
            else DrawCookie(canvas, 356, 370, 201, productIndex, view, flavor);
        }
        canvas.Flush();
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 80)
            ?? throw new InvalidOperationException("Could not encode sample artwork.");
        return encoded.ToArray();
    }

    private static void DrawCookie(SKCanvas canvas, float x, float y, float radius, int seed, int view, string flavor)
    {
        var random = new Random(1703 + seed * 37 + view * 19);
        var dark = flavor is "dark-chocolate" or "brownie" or "coffee" or "sea-salt";
        var red = flavor == "red-velvet";
        var pistachio = flavor == "pistachio";
        var dough = SKColor.Parse(dark ? "#674031" : red ? "#a34842" : pistachio ? "#c8b46c" : "#d7a665");
        using var shadow = new SKPaint { IsAntialias = true, Color = new SKColor(60, 30, 10, 45) };
        canvas.DrawCircle(x + 7, y + 10, radius, shadow);
        using var cookie = new SKPaint { IsAntialias = true, Color = dough };
        canvas.DrawCircle(x, y, radius, cookie);
        using var edge = new SKPaint { IsAntialias = true, Color = SKColor.Parse(dark ? "#4e3027" : red ? "#8e3935" : "#bc884c"), Style = SKPaintStyle.Stroke, StrokeWidth = radius * .035f };
        canvas.DrawCircle(x, y, radius * .98f, edge);
        for (var i = 0; i < 90; i++)
        {
            var angle = random.NextDouble() * Math.PI * 2;
            var distance = Math.Sqrt(random.NextDouble()) * radius * .91;
            using var texture = new SKPaint { IsAntialias = true, Color = new SKColor(255, 232, 186, (byte)random.Next(15, 55)) };
            canvas.DrawCircle(x + (float)(Math.Cos(angle) * distance), y + (float)(Math.Sin(angle) * distance), (float)random.NextDouble() * radius * .027f + 1, texture);
        }
        var topping = flavor switch
        {
            "pistachio" => "#617441", "white-chocolate" or "cranberry" or "red-velvet" => "#f5e2b5",
            "walnut" or "almond" or "peanut" or "oat" => "#a87947", "orange" => "#e58737",
            "lemon" => "#e6cc68", "coconut" => "#fff2dc", "vanilla" or "cinnamon" => "#b68049",
            "caramel" => "#b76c31", _ => "#493126"
        };
        for (var i = 0; i < 20; i++)
        {
            var angle = random.NextDouble() * Math.PI * 2;
            var distance = Math.Sqrt(random.NextDouble()) * radius * .81;
            var px = x + (float)(Math.Cos(angle) * distance);
            var py = y + (float)(Math.Sin(angle) * distance);
            var size = radius * (float)(.025 + random.NextDouble() * .047);
            using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(flavor == "cranberry" && i % 2 == 0 ? "#a83d4c" : topping) };
            canvas.Save(); canvas.Translate(px, py); canvas.RotateDegrees(random.Next(180));
            canvas.DrawRoundRect(new SKRect(-size, -size * .65f, size, size * .65f), size * .3f, size * .3f, paint);
            canvas.Restore();
        }
    }

    private static void DrawRect(SKCanvas canvas, SKRect rectangle, float radius, string color)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(color) };
        canvas.DrawRoundRect(rectangle, radius, radius, paint);
    }
}
