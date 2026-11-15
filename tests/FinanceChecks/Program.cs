using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CookieShop.Infra.Extensions;
using CookieShop.Infra.Data;
using CookieShop.Infra.Services;
using CookieShop.Infra.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using CookieShop.Domain.Constants;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var connection = Environment.GetEnvironmentVariable("FINANCE_TEST_DATABASE") ?? throw new Exception("Set FINANCE_TEST_DATABASE to an isolated PostgreSQL database.");
await using var db = new CookieShopDbContext(new DbContextOptionsBuilder<CookieShopDbContext>().UseNpgsql(connection).Options);
if (args.Contains("--migration")) await MigrationChecks.Run(db);
await db.Database.MigrateAsync();
var customerRole = await Role(UserRole.Customer);
var adminRole = await Role(UserRole.Admin);
var admin = await Customer(adminOnly: true);
var finance = new CustomerFinanceService(db);
var repository = new OrderRepository(db, finance);
var gateway = new TestGateway();
var payments = new OrderPaymentService(db, finance, [gateway]);
var checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
async Task Reject(Func<Task> action, string name)
{
    try { await action(); } catch (Exception e) when (e is ConflictException or NotFoundException) { db.ChangeTracker.Clear(); Check(true, name); return; }
    throw new Exception("Expected rejection: " + name);
}
async Task<Guid> Role(string name)
{
    var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == name);
    if (role is null) { role = new(name) { Id = Guid.NewGuid(), NormalizedName = name.ToUpperInvariant() }; db.Roles.Add(role); await db.SaveChangesAsync(); }
    return role.Id;
}
async Task<Guid> Customer(bool adminOnly = false)
{
    var id = Guid.NewGuid();
    db.Users.Add(new ApplicationUser { Id = id, UserName = id.ToString() });
    db.UserRoles.Add(new() { UserId = id, RoleId = adminOnly ? adminRole : customerRole });
    await db.SaveChangesAsync(); return id;
}
async Task<Order> Place(Guid user, decimal total = 500, bool later = false)
{
    var address = new Address { Id = Guid.NewGuid(), UserId = user, Title = "Test", AddressLine1 = "Test address", PhoneNumber = "09120000000" };
    var category = new Category { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
    var product = new Product { Id = Guid.NewGuid(), Name = "Cookie", Price = total, StockQuantity = 10, Category = category };
    db.Addresses.Add(address); db.Products.Add(product);
    var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.UserId == user);
    if (cart is null) { cart = new() { UserId = user }; db.Carts.Add(cart); }
    cart.Items.Add(new() { Product = product, Quantity = 1 });
    await db.SaveChangesAsync();
    return await repository.PlaceOrder(user, address.Id, null, later);
}
async Task Fund(Guid user, decimal amount, string? reference = null) => await finance.RecordPayment(user, admin, new(amount, reference ?? Guid.NewGuid().ToString()), default);
async Task Enable(Guid user, decimal? limit = 1000, DateTimeOffset? due = null) => await finance.SetTerms(user, new(limit, due, AllowPayLater: true), default);
async Task<Order> Read(Guid id) => (await repository.GetById(id))!;

var user = await Customer();
Check(!(await finance.GetAccount(user)).AllowPayLater, "Pay later is disabled by default");
await Reject(async () => await Place(await Customer(), later: true), "Customer cannot opt into pay later");
var order = await Place(user);
Check(order.Status == OrderStatus.Pending && order.PaymentStatus == PaymentStatus.Unpaid, "Pending unpaid checkout does not create debt");
Check((await finance.GetAccount(user)).Debt == 0, "Unpaid checkout is not account debt");
await Reject(() => repository.UpdateStatus(order.Id, OrderStatus.Accepted), "Prepaid order cannot be accepted unpaid");
await Fund(user, 200, "fund-1"); await Fund(user, 200, "fund-1");
Check((await finance.GetAccount(user)).Credit == 200, "Payment retries do not double-credit");
await Reject(() => Fund(user, 300, "fund-1"), "Conflicting payment reference is rejected");
await payments.PayWithWallet(user, order.Id, default);
Check((await Read(order.Id)).PaymentStatus == PaymentStatus.PartiallyPaid, "Wallet supports partial order funding");
var checkout = await payments.Start(user, order.Id, "test", default);
gateway.Succeed(checkout.AttemptId);
await payments.Verify(checkout.AttemptId, default); await payments.Verify(checkout.AttemptId, default);
order = await Read(order.Id);
Check(order.PaymentStatus == PaymentStatus.Paid && order.Status == OrderStatus.Pending && order.PaidAmount == 500, "Verified payment is independent of acceptance and callback retries are idempotent");
await repository.UpdateStatus(order.Id, OrderStatus.Rejected);
Check((await finance.GetAccount(user)).Credit == 500 && (await Read(order.Id)).PaymentStatus == PaymentStatus.Refunded, "Rejecting paid order refunds wallet");
await repository.UpdateStatus(order.Id, OrderStatus.Rejected);
Check((await finance.GetAccount(user)).Credit == 500, "Repeated rejection does not duplicate refund");
var stock = await db.Products.AsNoTracking().SingleAsync(x => x.Id == order.Items.First().ProductId);
Check(stock.StockQuantity == 10, "Rejected order restores reserved inventory once");

var debtor = await Customer(); await Enable(debtor, due: DateTimeOffset.UtcNow.AddDays(-1));
var debtOrder = await Place(debtor, later: true);
await repository.UpdateStatus(debtOrder.Id, OrderStatus.Accepted);
Check((await finance.GetAccount(debtor)).Debt == 500, "Approved pay later creates debt on acceptance");
Check((await finance.GetAccount(debtor)).OrderingBlockReason!.StartsWith("OVERDUE_DEBT"), "Outstanding overdue allocation blocks credit ordering");
await Fund(debtor, 200);
Check((await Read(debtOrder.Id)).PaidAmount == 200 && (await finance.GetAccount(debtor)).Debt == 300, "Received funds allocate to order debt");
await repository.UpdateStatus(debtOrder.Id, OrderStatus.Cancelled);
Check((await finance.GetAccount(debtor)).Credit == 200, "Cancelling partially paid debt refunds only money actually received");

var unpaid = await Customer(); await Enable(unpaid);
var first = await Place(unpaid, later: true); await repository.UpdateStatus(first.Id, OrderStatus.Accepted);
var second = await Place(unpaid, later: true); await repository.UpdateStatus(second.Id, OrderStatus.Accepted);
await repository.UpdateStatus(first.Id, OrderStatus.Cancelled);
Check((await Read(second.Id)).PaidAmount == 0 && (await finance.GetAccount(unpaid)).Debt == 500, "Cancelling unpaid debt does not invent funds for another order");
await Reject(() => finance.EnsureCanOrder(unpaid, 501), "Projected credit limit is enforced");
await Fund(unpaid, 500);
Check((await Read(second.Id)).PaymentStatus == PaymentStatus.Paid && (await finance.GetAccount(unpaid)).Debt == 0, "Payment fully settles accepted debt");

var lateUser = await Customer(); var late = await Place(lateUser);
var lateAttempt = await payments.Start(lateUser, late.Id, "test", default);
await repository.Cancel(late.Id); gateway.Succeed(lateAttempt.AttemptId);
await payments.Verify(lateAttempt.AttemptId, default);
Check((await finance.GetAccount(lateUser)).Credit == 500 && (await Read(late.Id)).Status == OrderStatus.Cancelled, "Late verified payment credits wallet without reviving cancelled order");
await payments.Verify(lateAttempt.AttemptId, default);
Check((await finance.GetAccount(lateUser)).Credit == 500, "Late callback retry cannot duplicate credit");

var failedUser = await Customer(); var failedOrder = await Place(failedUser);
var failed = await payments.Start(failedUser, failedOrder.Id, "test", default);
await payments.Verify(failed.AttemptId, default);
Check((await Read(failedOrder.Id)).PaymentStatus == PaymentStatus.Unpaid && (await finance.GetAccount(failedUser)).Balance == 0, "Failed attempt leaves order unpaid and wallet unchanged");
var mismatch = await payments.Start(failedUser, failedOrder.Id, "test", default);
gateway.Succeed(mismatch.AttemptId, wrongAmount: true);
await Reject(() => payments.Verify(mismatch.AttemptId, default), "Mismatched verified amount cannot credit account");

var duplicateUser = await Customer(); var duplicateOrder = await Place(duplicateUser);
var a = await payments.Start(duplicateUser, duplicateOrder.Id, "test", default);
var b = await payments.Start(duplicateUser, duplicateOrder.Id, "test", default);
gateway.Succeed(a.AttemptId); gateway.Succeed(b.AttemptId);
await payments.Verify(a.AttemptId, default); await payments.Verify(b.AttemptId, default);
Check((await Read(duplicateOrder.Id)).PaidAmount == 500 && (await finance.GetAccount(duplicateUser)).Credit == 500, "Two successful attempts allocate order once and preserve excess as wallet credit");

await Reject(() => finance.GetAccount(admin), "Admins have no customer wallet");
await Reject(() => finance.SetTerms(admin, new(1000, null, AllowPayLater: true), default), "Admins cannot receive customer payment terms");
db.UserRoles.Add(new() { UserId = admin, RoleId = customerRole }); await db.SaveChangesAsync();
await Reject(() => finance.GetAccount(admin), "Admin exclusion holds with both roles");
await Reject(() => payments.PayWithWallet(user, duplicateOrder.Id, default), "Wallet payments enforce order ownership");
await Reject(() => payments.Start(user, duplicateOrder.Id, "test", default), "Gateway initiation enforces order ownership");
var settings = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["JwtSettings:SecretKey"] = new string('x', 64), ["JwtSettings:Issuer"] = "tests", ["JwtSettings:Audience"] = "tests", ["JwtSettings:ExpirationMinutes"] = "30"
}).Build();
var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
services.AddLogging(); services.AddJwtAuthentication(settings);
await using (var provider = services.BuildServiceProvider())
{
    var authorization = provider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
    foreach (var roleSet in new[] { new[] { UserRole.Customer }, new[] { UserRole.Admin }, new[] { UserRole.Customer, UserRole.Admin } })
    {
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            roleSet.Select(x => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, x)), "test"));
        var result = await authorization.AuthorizeAsync(principal, null, "CustomerOnly");
        Check(result.Succeeded == (roleSet.Length == 1 && roleSet[0] == UserRole.Customer), "CustomerOnly policy for " + string.Join("+", roleSet));
    }
}
Console.WriteLine($"All {checks} finance checks passed.");

sealed class TestGateway : IPaymentGateway
{
    public string Provider => "test";
    private readonly Dictionary<Guid, (decimal Amount, bool Paid, bool Wrong)> attempts = new();
    public void Succeed(Guid id, bool wrongAmount = false) => attempts[id] = (attempts[id].Amount, true, wrongAmount);
    public Task<GatewayCheckout> Create(Guid attemptId, decimal amount, CancellationToken ct)
    {
        attempts.Add(attemptId, (amount, false, false));
        return Task.FromResult(new GatewayCheckout(attemptId.ToString(), "https://gateway.example/checkout"));
    }
    public Task<GatewayVerification> Verify(string authority, CancellationToken ct)
    {
        var attempt = attempts[Guid.Parse(authority)];
        return Task.FromResult(new GatewayVerification(attempt.Paid, attempt.Amount + (attempt.Wrong ? 1 : 0), attempt.Paid ? authority : null));
    }
}
