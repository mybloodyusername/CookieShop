using CookieShop.Infra.Data;
using CookieShop.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

static class MigrationChecks
{
    // Run only against a new, isolated database. Never downgrade an existing database to create fixtures.
    public static async Task Run(CookieShopDbContext db)
    {
        if ((await db.Database.GetAppliedMigrationsAsync()).Any())
            throw new Exception("Migration checks require an empty isolated database.");
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261113221849_AddCustomerPaymentTerms");
        var user = Guid.NewGuid(); var address = Guid.NewGuid();
        var charged = Guid.NewGuid(); var paid = Guid.NewGuid(); var pending = Guid.NewGuid(); var ambiguous = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "AspNetUsers" ("Id","FirstName","LastName","CreatedAt","UpdatedAt","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount")
            VALUES ({user},'','',now(),now(),false,false,false,false,0);
            INSERT INTO "Addresses" ("Id","UserId","Title","AddressLine1","AddressLine2","PhoneNumber")
            VALUES ({address},{user},'Test','Test','','09120000000');
            INSERT INTO "Orders" ("Id","UserId","AddressId","ShippingTitle","ShippingAddressLine1","ShippingAddressLine2","ShippingPhoneNumber","Status","TotalAmount","CreatedAt","UpdatedAt") VALUES
            ({charged},{user},{address},'','','','','Accepted',500,now()-interval '2 days',now()),
            ({paid},{user},{address},'','','','','Paid',100,now()-interval '1 day',now()),
            ({pending},{user},{address},'','','','','Pending',300,now(),now()),
            ({ambiguous},{user},{address},'','','','','Delivered',100,now(),now());
            INSERT INTO "FinancialEntries" ("Id","UserId","OrderId","Amount","Kind","Reference","CreatedAt") VALUES
            (gen_random_uuid(),{user},{charged},-500,'OrderCharge','',now()),
            (gen_random_uuid(),{user},NULL,200,'Payment','legacy-receipt',now());
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "Orders" SET "CreatedAt" = '2026-03-20 20:30:00+00' WHERE "Id" = {paid};
            """);
        try { await db.Database.MigrateAsync(); throw new Exception("Ambiguous legacy fulfilment was accepted."); }
        catch (PostgresException e) when (e.MessageText.StartsWith("Reconcile legacy fulfilled"))
        { Console.WriteLine("PASS Migration refuses ambiguous historical fulfilment"); }
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"Orders\" WHERE \"Id\" = {ambiguous}");
        await db.Database.MigrateAsync();
        var orders = await db.Orders.AsNoTracking().Where(x => x.UserId == user).ToDictionaryAsync(x => x.Id);
        if (!orders[paid].OrderNumber.StartsWith("14050101-") || orders.Values.Any(x => x.OrderNumber.Length != 15) ||
            orders.Values.Select(x => x.OrderNumber).Distinct().Count() != orders.Count)
            throw new Exception("Historical order numbers were not backfilled correctly.");
        Console.WriteLine("PASS Migration backfills unique Persian-date order numbers at Tehran Nowruz midnight");
        if (orders[charged].PaidAmount != 200 || orders[charged].PaymentStatus != PaymentStatus.PartiallyPaid || !orders[charged].PayLater)
            throw new Exception("Legacy funds were not allocated to the charged order.");
        Console.WriteLine("PASS Migration allocates legacy pooled funds to charged orders");
        if (orders[paid].Status != OrderStatus.Accepted || orders[paid].PaymentStatus != PaymentStatus.Paid || orders[paid].PaidAmount != 100)
            throw new Exception("Legacy Paid order was not converted.");
        Console.WriteLine("PASS Migration separates legacy Paid status");
        if (orders[pending].PaymentStatus != PaymentStatus.Unpaid || orders[pending].PaidAmount != 0)
            throw new Exception("Legacy pending order was funded without evidence.");
        Console.WriteLine("PASS Migration preserves unfunded pending orders");
        if (await db.FinancialEntries.Where(x => x.UserId == user).SumAsync(x => x.Amount) != -300)
            throw new Exception("Migration changed the account balance.");
        Console.WriteLine("PASS Migration preserves the account balance");
        if (await db.OrderPayments.Where(x => x.Order.UserId == user).SumAsync(x => x.Amount) != 300)
            throw new Exception("Migration payment allocations are incomplete.");
        Console.WriteLine("PASS Migration persists order allocation history");
    }
}
