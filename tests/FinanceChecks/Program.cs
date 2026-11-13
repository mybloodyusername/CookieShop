using CookieShop.Infra.Data;
using CookieShop.Infra.Services;
using CookieShop.Domain.Entities;
using CookieShop.App.DTOs.Finance;
using CookieShop.App.Exceptions;
using Microsoft.EntityFrameworkCore;
var connection = Environment.GetEnvironmentVariable("FINANCE_TEST_DATABASE") ?? throw new Exception("Set FINANCE_TEST_DATABASE to an isolated PostgreSQL database.");
await using var db = new CookieShopDbContext(new DbContextOptionsBuilder<CookieShopDbContext>().UseNpgsql(connection).Options);
await db.Database.MigrateAsync();
var id = Guid.NewGuid();
db.Users.Add(new ApplicationUser { Id = id, UserName = id.ToString() });
await db.SaveChangesAsync();
var service = new CustomerFinanceService(db);
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); }
await service.SetTerms(id, new(1000, DateTimeOffset.UtcNow.AddDays(1)), default);
await service.RecordPayment(id, id, new(200, "payment-1"), default);
await service.RecordPayment(id, id, new(200, "payment-1"), default);
Check((await service.GetAccount(id)).Balance == 200, "Payment retries do not double-credit");
try { await service.RecordPayment(id, id, new(300, "payment-1"), default); throw new Exception("Duplicate reference accepted"); } catch (ConflictException) { Console.WriteLine("PASS conflicting reference rejected"); }
db.FinancialEntries.Add(new() { UserId = id, Amount = -500, Kind = "OrderCharge", DueAt = DateTimeOffset.UtcNow.AddDays(-1) });
await db.SaveChangesAsync();
Check((await service.GetAccount(id)).OrderingBlockReason!.StartsWith("OVERDUE_DEBT"), "Overdue balance blocks ordering");
await service.RecordPayment(id, id, new(300, "payment-2"), default);
Check((await service.GetAccount(id)).Balance == 0, "Payment settles debt");
db.FinancialEntries.Add(new() { UserId = id, Amount = -100, Kind = "OrderCharge", DueAt = DateTimeOffset.UtcNow.AddDays(1) });
await db.SaveChangesAsync();
Check((await service.GetAccount(id)).OrderingBlockReason == null, "Settled old charges do not make new debt overdue");
try { await service.EnsureCanOrder(id, 901); throw new Exception("Limit not enforced"); } catch (ConflictException) { Console.WriteLine("PASS projected limit enforced"); }
Check((await service.History(id, 1, default)).Count == 4, "Statement retains financial history");
Console.WriteLine("All finance checks passed.");
