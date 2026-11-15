using System.Globalization;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class OrderNumberChecks
{
    public static void Run(Action<bool, string> check)
    {
        var before = DateTimeOffset.Parse("2026-03-20T20:29:59Z", CultureInfo.InvariantCulture);
        check(OrderNumberGenerator.Generate(before).StartsWith("14041229-") &&
              OrderNumberGenerator.Generate(before.AddSeconds(1)).StartsWith("14050101-"),
            "Persian year rolls over at Tehran midnight");
        var oldCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fa-IR");
            var numbers = Enumerable.Range(0, 1000).Select(_ => OrderNumberGenerator.Generate(before)).ToList();
            check(numbers.All(n => System.Text.RegularExpressions.Regex.IsMatch(n, @"^14041229-[0-9]{6}$")) &&
                  numbers.Any(n => n[9] == '0'), "Random suffix preserves leading zeros and invariant ASCII digits");
        }
        finally { CultureInfo.CurrentCulture = oldCulture; }
    }
}

sealed class OrderNumberCollisionInterceptor : SaveChangesInterceptor
{
    public string? DuplicateNumber { get; set; }
    public int Attempts { get; private set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var order = eventData.Context?.ChangeTracker.Entries<Order>().SingleOrDefault(e => e.State == EntityState.Added);
        if (DuplicateNumber is not null && order is not null)
        {
            Attempts++;
            if (Attempts == 1) order.Entity.OrderNumber = DuplicateNumber;
            else DuplicateNumber = null;
        }
        return ValueTask.FromResult(result);
    }
}
