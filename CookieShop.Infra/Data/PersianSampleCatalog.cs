using CookieShop.Domain.Entities;

namespace CookieShop.Infra.Data;

internal static class PersianSampleCatalog
{
    internal record Item(string Category, string Name, string Description, decimal Price,
        decimal? SalePrice, int Stock, bool Available, string Flavor);

    internal static readonly string[] Categories =
    [
        "جعبه‌ها", "کوکی‌ها", "رژیمی"
    ];

    internal static readonly Item[] Items =
    [
        new(Categories[0], "جعبه پذیرایی کلاسیک", "جعبه‌ای از کوکی‌های ساده و خوش‌عطر برای پذیرایی روزانه.", 680000, null, 24, true, "assorted-box"),
        new(Categories[0], "جعبه هدیه شکلاتی", "ترکیبی از کوکی‌های شکلاتی با بسته‌بندی مناسب هدیه.", 790000, 740000, 18, true, "chocolate-box"),
        new(Categories[0], "جعبه دورهمی", "مجموعه‌ای متنوع برای دورهمی‌های خانوادگی و دوستانه.", 980000, null, 16, true, "family-box"),
        new(Categories[0], "جعبه ویژه فروشگاه", "جعبه‌ای دست‌چین‌شده از محبوب‌ترین طعم‌های فروشگاه.", 1180000, 1090000, 12, true, "persian-box"),

        new(Categories[1], "کوکی کره‌ای وانیلی", "کوکی لطیف با کره تازه و وانیل؛ هر بسته شامل ۴ عدد است.", 185000, null, 60, true, "vanilla"),
        new(Categories[1], "کوکی شکلات چیپسی", "کوکی کلاسیک با تکه‌های شکلات تلخ؛ هر بسته شامل ۴ عدد است.", 220000, 198000, 72, true, "chocolate-chip"),
        new(Categories[1], "کوکی دارچینی", "کوکی نرم و خوش‌عطر با دارچین و شکر قهوه‌ای.", 195000, null, 45, true, "cinnamon"),
        new(Categories[1], "کوکی جو دوسر و عسل", "ترکیب جو دوسر و عسل با بافتی ترد و دلپذیر.", 210000, null, 50, true, "oat"),
        new(Categories[1], "کوکی دبل چاکلت", "خمیر کاکائویی با تکه‌های شکلات تلخ و شیری.", 265000, 239000, 64, true, "dark-chocolate"),
        new(Categories[1], "کوکی شکلات سفید", "کوکی کره‌ای با تکه‌های شکلات سفید و رایحه وانیل.", 250000, null, 38, true, "white-chocolate"),
        new(Categories[1], "کوکی براونی", "کوکی شکلاتی با مرکز نرم و پوسته ترک‌خورده.", 285000, null, 42, true, "brownie"),
        new(Categories[1], "کوکی گردویی", "کوکی کره‌ای با گردوی خردشده و شکر قهوه‌ای.", 295000, null, 46, true, "walnut"),
        new(Categories[1], "کوکی پسته و زعفران", "ترکیب پسته ایرانی و زعفران در خمیری لطیف.", 380000, 349000, 32, true, "pistachio"),
        new(Categories[1], "کوکی پرتقال و شکلات", "رایحه پوست پرتقال تازه در کنار شکلات تلخ.", 255000, null, 48, true, "orange"),

        new(Categories[2], "کوکی جو دوسر رژیمی", "کوکی سبک با جو دوسر و شیرین‌کننده کم‌کالری.", 230000, null, 30, true, "oat"),
        new(Categories[2], "کوکی بدون شکر افزوده", "تهیه‌شده بدون شکر افزوده، مناسب میان‌وعده سبک.", 245000, null, 24, true, "cinnamon"),
        new(Categories[2], "کوکی پروتئینی", "کوکی مقوی با مغزها و پروتئین گیاهی.", 290000, 270000, 20, true, "almond")
    ];

    internal static List<Product> Create(IReadOnlyDictionary<string, Category> categories, DateTimeOffset now) =>
        Items.Select((item, index) => new Product
        {
            Id = Guid.NewGuid(), Category = categories[item.Category], Name = item.Name,
            Description = item.Description, Price = item.Price, SalePrice = item.SalePrice,
            IsOnSale = item.SalePrice != null, StockQuantity = item.Stock, IsAvailable = item.Available,
            CreatedAt = now.AddDays(-35).AddHours(index), UpdatedAt = now.AddDays(-1)
        }).ToList();
}
