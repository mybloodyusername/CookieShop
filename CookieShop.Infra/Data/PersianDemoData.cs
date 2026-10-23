using System.Security.Cryptography;
using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CookieShop.Infra.Data;

/// <summary>Explicit development-only reset, invoked by the --reset-demo-data command.</summary>
public static class PersianDemoData
{
    public static async Task ResetAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<CookieShopDbContext>();
        var logger = services.GetRequiredService<ILogger<CookieShopDbContext>>();
        await db.Database.OpenConnectionAsync(cancellationToken);
        // Serialize maintenance commands across both the cleanup and migration phases.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(7231062401)", cancellationToken);
        try
        {
            var adminIds = await (from membership in db.UserRoles
                join role in db.Roles on membership.RoleId equals role.Id
                where role.Name == UserRole.Admin
                select membership.UserId).Distinct().ToArrayAsync(cancellationToken);
            if (adminIds.Length == 0)
                throw new InvalidOperationException("No existing administrator found. Provision an owner before resetting sample data.");
            var admins = await db.Users.AsNoTracking().Where(x => adminIds.Contains(x.Id))
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
            var owner = admins[0];
            if (string.IsNullOrWhiteSpace(owner.PhoneNumber))
                throw new InvalidOperationException("The administrator needs a phone number for sample delivery addresses.");
            if (admins.Any(x => string.IsNullOrWhiteSpace(x.UserName) || !x.UserName.All(char.IsAscii)))
                throw new InvalidOperationException("Administrator usernames must use English/ASCII characters before resetting samples.");

            // Prepare all generated content before deleting any existing rows.
            var now = DateTimeOffset.UtcNow;
            var categories = PersianSampleCatalog.Categories.ToDictionary(x => x,
                x => new Category { Id = Guid.NewGuid(), Name = x });
            var products = PersianSampleCatalog.Create(categories, now);
            var images = PrepareImages(products, now);
            var addresses = CreateAddresses(owner);
            var orders = CreateOrders(owner.Id, addresses, products, now);
            var cart = new Cart
            {
                Id = Guid.NewGuid(), UserId = owner.Id,
                Items = new List<CartItem>
                {
                    new() { Id = Guid.NewGuid(), Product = products[1], Quantity = 2 },
                    new() { Id = Guid.NewGuid(), Product = products[9], Quantity = 1 },
                    new() { Id = Guid.NewGuid(), Product = products[20], Quantity = 1 }
                }
            };

            var tables = await db.Database.SqlQueryRaw<string>(
                "SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = 'public'")
                .ToListAsync(cancellationToken);
            if (tables.Contains("MediaImages") && await db.MediaImages.AnyAsync(x => x.StorageProvider != "Postgres", cancellationToken))
                throw new InvalidOperationException("This local reset supports PostgreSQL media only. Move or remove externally stored media before resetting.");

            // Old demo prices can violate the new whole-toman constraints. Clear the old rows
            // before applying those migrations. Identity credentials for admins are never deleted.
            await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
            {
                string[] resetTables = ["ProductImages", "ImageBlobs", "MediaImages", "OrderItems", "Orders",
                    "CartItems", "Carts", "Products", "Categories", "Addresses"];
                foreach (var table in resetTables.Where(tables.Contains))
                {
                    // Table names come exclusively from the fixed allowlist above.
                    var deleteSql = $"DELETE FROM public.\"{table}\"";
                    await db.Database.ExecuteSqlRawAsync(deleteSql, cancellationToken);
                }
                await db.Users.Where(x => !adminIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            // Migrations manage their own transaction and must not run inside the cleanup transaction.
            await db.Database.MigrateAsync(cancellationToken);
            db.ChangeTracker.Clear();

            await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
            {
                db.Categories.AddRange(categories.Values);
                db.Products.AddRange(products);
                db.MediaImages.AddRange(images.Select(x => x.Image));
                db.Addresses.AddRange(addresses);
                db.Orders.AddRange(orders);
                db.Carts.Add(cart);
                foreach (var admin in await db.Users.Where(x => adminIds.Contains(x.Id)).ToListAsync(cancellationToken))
                {
                    if (string.IsNullOrWhiteSpace(admin.FirstName)) admin.FirstName = "مدیر";
                    if (string.IsNullOrWhiteSpace(admin.LastName)) admin.LastName = "فروشگاه";
                    admin.UpdatedAt = now;
                }
                await db.SaveChangesAsync(cancellationToken);
                db.ImageBlobs.AddRange(images.Select(x => new ImageBlob { Id = x.Image.Id, Data = x.Bytes }));
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            logger.LogInformation(
                "Persian sample setup complete: {Admins} admin(s) preserved, 0 customers, {Categories} categories, {Products} products, {Images} WebP images, {Addresses} addresses, {Orders} orders, 1 cart. All amounts are whole toman.",
                admins.Count, categories.Count, products.Count, images.Count, addresses.Length, orders.Count);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(7231062401)", CancellationToken.None);
            await db.Database.CloseConnectionAsync();
        }
    }

    private static List<(MediaImage Image, byte[] Bytes)> PrepareImages(List<Product> products, DateTimeOffset now)
    {
        var images = new List<(MediaImage, byte[])>();
        for (var index = 0; index < products.Count; index++)
        {
            var product = products[index];
            for (var view = 0; view < 2; view++)
            {
                var bytes = SampleProductArtwork.Create(PersianSampleCatalog.Items[index].Flavor, index, view);
                var image = new MediaImage
                {
                    Alt = $"تصویر نمونه {product.Name}، {(view == 0 ? "نمای بسته‌بندی و محصول" : "نمای نزدیک محصول")}",
                    Album = product.Category.Name, StorageProvider = "Postgres", Width = 720, Height = 720,
                    ByteLength = bytes.Length, Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                    CreatedAt = now.AddDays(-10).AddMinutes(index * 2 + view)
                };
                image.StorageKey = image.Id.ToString("D");
                image.FileName = $"{image.Id:D}.webp";
                product.Images.Add(new ProductImage { Product = product, Image = image, SortOrder = view });
                images.Add((image, bytes));
            }
        }
        return images;
    }

    private static Address[] CreateAddresses(ApplicationUser owner) =>
    [
        new() { Id = Guid.NewGuid(), UserId = owner.Id, Title = "خانه نمونه", PhoneNumber = owner.PhoneNumber!,
            AddressLine1 = "تهران، خیابان ولیعصر، خیابان نمونه، پلاک ۱۲", AddressLine2 = "واحد ۳؛ نشانی نمایشی برای سفارش‌های نمونه" },
        new() { Id = Guid.NewGuid(), UserId = owner.Id, Title = "محل کار نمونه", PhoneNumber = owner.PhoneNumber!,
            AddressLine1 = "تهران، میدان ونک، خیابان نمونه، ساختمان آفتاب", AddressLine2 = "طبقه ۲، واحد ۵؛ نشانی نمایشی" },
        new() { Id = Guid.NewGuid(), UserId = owner.Id, Title = "نشانی هدیه نمونه", PhoneNumber = owner.PhoneNumber!,
            AddressLine1 = "کرج، گوهردشت، خیابان نمونه، پلاک ۲۴", AddressLine2 = "واحد ۱؛ نشانی نمایشی برای بسته هدیه" }
    ];

    private record OrderDraft(OrderStatus Status, int DaysAgo, int Address, string Note, (int Product, int Quantity)[] Lines);

    private static List<Order> CreateOrders(Guid ownerId, Address[] addresses, List<Product> products, DateTimeOffset now)
    {
        OrderDraft[] drafts =
        [
            new(OrderStatus.Delivered, 24, 0, "سفارش نمونه تحویل‌شده؛ بسته‌ها سالم به دست گیرنده رسیدند.", [(0, 2), (1, 1), (4, 1)]),
            new(OrderStatus.Delivered, 20, 1, "سفارش نمونه برای پذیرایی در محل کار؛ تحویل در ساعت اداری.", [(20, 2), (17, 1)]),
            new(OrderStatus.Delivered, 16, 2, "هدیه نمونه تولد؛ بسته‌بندی هدیه و کارت تبریک همراه سفارش.", [(21, 1), (9, 2)]),
            new(OrderStatus.Shipped, 8, 0, "سفارش نمونه ارسال‌شده؛ بسته در مسیر تحویل است.", [(8, 2), (12, 1), (15, 1)]),
            new(OrderStatus.Shipped, 6, 2, "بسته هدیه نمونه؛ لطفاً پیش از تحویل با گیرنده هماهنگ شود.", [(22, 1), (5, 1)]),
            new(OrderStatus.Paid, 4, 1, "سفارش نمونه با وضعیت پرداخت‌شده؛ در انتظار آماده‌سازی برای ارسال.", [(23, 1), (4, 2)]),
            new(OrderStatus.Paid, 3, 0, "سفارش نمونه با پرداخت نمایشی؛ لطفاً طعم‌ها جداگانه بسته‌بندی شوند.", [(6, 1), (10, 1), (13, 2)]),
            new(OrderStatus.Pending, 2, 0, "سفارش نمونه در انتظار پرداخت؛ موجودی اقلام رزرو شده است.", [(1, 2), (9, 1)]),
            new(OrderStatus.Pending, 1, 1, "سفارش نمونه دورهمی؛ در انتظار تکمیل پرداخت.", [(20, 1), (2, 1), (11, 1)]),
            new(OrderStatus.Pending, 0, 2, "سفارش نمونه هدیه؛ در انتظار پرداخت و تأیید نهایی.", [(22, 1), (16, 1)]),
            new(OrderStatus.Cancelled, 12, 0, "سفارش نمونه لغوشده به درخواست خریدار؛ موجودی اقلام آزاد شده است.", [(3, 2), (14, 1)]),
            new(OrderStatus.Cancelled, 5, 1, "سفارش نمونه لغوشده؛ پرداخت تکمیل نشد و موجودی رزرو نیست.", [(7, 1), (17, 1), (21, 1)])
        ];
        return drafts.Select(draft =>
        {
            var address = addresses[draft.Address];
            var created = now.AddDays(-draft.DaysAgo).AddHours(-6);
            var order = new Order
            {
                Id = Guid.NewGuid(), UserId = ownerId, Address = address, Status = draft.Status, Note = draft.Note,
                ShippingTitle = address.Title, ShippingAddressLine1 = address.AddressLine1,
                ShippingAddressLine2 = address.AddressLine2, ShippingPhoneNumber = address.PhoneNumber,
                CreatedAt = created, UpdatedAt = draft.Status == OrderStatus.Pending ? created : created.AddHours(2)
            };
            foreach (var (productIndex, quantity) in draft.Lines)
            {
                var product = products[productIndex];
                var price = product.IsOnSale ? product.SalePrice!.Value : product.Price;
                order.Items.Add(new OrderItem { Id = Guid.NewGuid(), Product = product, ProductName = product.Name, UnitPrice = price, Quantity = quantity });
                order.TotalAmount += price * quantity;
                if (draft.Status != OrderStatus.Cancelled) product.StockQuantity -= quantity;
            }
            return order;
        }).ToList();
    }
}
