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
var numberCollision = new OrderNumberCollisionInterceptor();
await using var db = new CookieShopDbContext(new DbContextOptionsBuilder<CookieShopDbContext>().UseNpgsql(connection).AddInterceptors(numberCollision).Options);
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
async Task<(Guid Address, Guid Product)> Prepare(Guid user, decimal total = 500)
{
    var address = new Address { Id = Guid.NewGuid(), UserId = user, Title = "Test", AddressLine1 = "Test address", PhoneNumber = "09120000000" };
    var category = new Category { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };
    var product = new Product { Id = Guid.NewGuid(), Name = "Cookie", Price = total, StockQuantity = 10, Category = category };
    db.Addresses.Add(address); db.Products.Add(product);
    var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.UserId == user);
    if (cart is null) { cart = new() { UserId = user }; db.Carts.Add(cart); }
    cart.Items.Add(new() { Product = product, Quantity = 1 });
    await db.SaveChangesAsync();
    return (address.Id, product.Id);
}
async Task<Order> Place(Guid user, decimal total = 500, bool later = false)
{
    var prepared = await Prepare(user, total);
    return await repository.PlaceOrder(user, prepared.Address, null, later);
}
async Task Fund(Guid user, decimal amount, string? reference = null) => await finance.RecordPayment(user, admin,
    new(amount, reference ?? Guid.NewGuid().ToString(), "BankTransfer", "Test receipt"), default);
async Task Enable(Guid user, decimal? limit = 5000, DateTimeOffset? due = null) => await finance.SetTerms(user, new(limit, due, AllowPayLater: true), default);
async Task<Order> Read(Guid id) => (await repository.GetById(id))!;
Task<Order?> Status(Guid id, OrderStatus status, string reason = "Test status change") => repository.UpdateStatus(id, status, admin, reason);
async Task RejectInput(Func<Task> action, string name)
{
    try { await action(); } catch (System.ComponentModel.DataAnnotations.ValidationException) { db.ChangeTracker.Clear(); Check(true, name); return; }
    throw new Exception("Expected invalid input: " + name);
}
async Task<CookieShop.App.DTOs.Finance.FinancialEntryResponse> Refund(Guid user, decimal amount, Guid? orderId = null, string? reference = null) =>
    await finance.RecordRefund(user, admin, new(amount, reference ?? Guid.NewGuid().ToString(), "BankTransfer", "Returned to customer", orderId), default);

var user = await Customer();
Check(!(await finance.GetAccount(user)).AllowPayLater, "Pay later is disabled by default");
var prepared = await Prepare(user);
await Reject(() => repository.PlaceOrder(user, prepared.Address, null), "Unfunded prepaid checkout is rejected before order creation");
Check(!await db.Orders.AnyAsync(x => x.UserId == user) && await db.Products.Where(x => x.Id == prepared.Product).Select(x => x.StockQuantity).SingleAsync() == 10 &&
    await db.CartItems.AnyAsync(x => x.Cart.UserId == user), "Rejected checkout preserves cart and stock and creates no order");
await Fund(user, 499, "fund-1"); await Fund(user, 499, "fund-1");
Check((await finance.GetAccount(user)).Credit == 499, "Receipt retries do not double-credit");
await Reject(() => Fund(user, 300, "fund-1"), "Conflicting receipt reference is rejected");
await Reject(() => repository.PlaceOrder(user, prepared.Address, null), "Partial prepayment cannot create an order");
await Reject(() => repository.PlaceOrder(user, prepared.Address, null, true), "Customers cannot grant themselves credit");
await Fund(user, 1);
var order = await repository.PlaceOrder(user, prepared.Address, null);
Check(order.PaidAmount == 500 && order.PaymentStatus == PaymentStatus.Paid && (await finance.GetAccount(user)).Credit == 0,
    "Prepaid order is fully funded atomically at creation");
Check(System.Text.RegularExpressions.Regex.IsMatch(order.OrderNumber, @"^[0-9]{8}-[0-9]{6}$"), "Checkout assigns a fixed-width order number");
var numberOwner = await Customer(); await Fund(numberOwner, 500);
numberCollision.DuplicateNumber = order.OrderNumber;
var collisionOrder = await Place(numberOwner);
Check(numberCollision.Attempts == 2 && collisionOrder.OrderNumber != order.OrderNumber && collisionOrder.PaidAmount == 500 &&
    await db.OrderPayments.CountAsync(x => x.OrderId == collisionOrder.Id) == 1, "Order-number collision retries without charging twice");
Check(await db.Products.Where(x => x.Id == collisionOrder.Items.First().ProductId).Select(x => x.StockQuantity).SingleAsync() == 9 &&
    !await db.CartItems.AnyAsync(x => x.Cart.UserId == numberOwner), "Collision retry reserves stock once and clears the cart");
OrderNumberChecks.Run(Check);
await using (var immutableDb = new CookieShopDbContext(new DbContextOptionsBuilder<CookieShopDbContext>().UseNpgsql(connection).Options))
{
    var savedOrder = await immutableDb.Orders.SingleAsync(x => x.Id == order.Id);
    savedOrder.OrderNumber = "14050101-000000";
    try { await immutableDb.SaveChangesAsync(); throw new Exception("Saved order number was editable."); }
    catch (InvalidOperationException e) when (e.Message.Contains("OrderNumber")) { Check(true, "Saved order numbers cannot be changed through EF"); }
}
await RejectInput(() => Status(order.Id, OrderStatus.Rejected, " "), "Rejection requires a reason");
await Status(order.Id, OrderStatus.Rejected, "Cannot fulfil this item");
await Status(order.Id, OrderStatus.Rejected, "Cannot fulfil this item");
var rejected = await Read(order.Id);
Check((await finance.GetAccount(user)).Credit == 500 && rejected.PaymentStatus == PaymentStatus.WalletCredited && rejected.StatusChanges.Count == 2 &&
    rejected.StatusChanges.Last(x => x.ToStatus == OrderStatus.Rejected).ActorId == admin, "Rejection releases wallet credit once and records actor and reason");
Check(await db.Products.Where(x => x.Id == prepared.Product).Select(x => x.StockQuantity).SingleAsync() == 10, "Rejection restores stock once");
var refundReceipt = await Refund(user, 200, order.Id, "refund-1");
await Refund(user, 200, order.Id, "refund-1");
Check((await finance.GetAccount(user)).Credit == 300 && (await Read(order.Id)).RefundedAmount == 200 &&
    (await Read(order.Id)).PaymentStatus == PaymentStatus.PartiallyRefunded && refundReceipt.Amount == -200 && refundReceipt.RecordedBy == admin,
    "Partial external refund reduces wallet once and records audit details");
await Reject(() => Refund(user, 201, order.Id, "refund-1"), "Conflicting refund retries are rejected");
await Reject(() => Refund(user, 301, order.Id), "Over-refunding a paid order is rejected");
await Refund(user, 300, order.Id);
Check((await finance.GetAccount(user)).Credit == 0 && (await Read(order.Id)).PaymentStatus == PaymentStatus.Refunded, "Full external refund is distinct from wallet credit");

var debtor = await Customer(); await Enable(debtor);
var first = await Place(debtor, 200, later: true); var second = await Place(debtor, 300, later: true); var third = await Place(debtor, 400, later: true);
Check((await finance.GetAccount(debtor)).Debt == 0, "Pending approved-credit orders do not yet create debt");
await Status(first.Id, OrderStatus.Accepted); await Status(second.Id, OrderStatus.Accepted); await Status(third.Id, OrderStatus.Accepted);
Check((await finance.GetAccount(debtor)).Debt == 900, "Accepted credit orders create debt");
var group = new CookieShop.App.DTOs.Finance.RecordPaymentRequest(500, "group-1", "Cash", "Cash received against selected invoices",
    [new(second.Id, 150), new(third.Id, 250)], DateTimeOffset.UtcNow.AddHours(-1));
var groupReceipt = await finance.RecordPayment(debtor, admin, group, default);
await finance.RecordPayment(debtor, admin, group, default);
var account = await finance.GetAccount(debtor);
Check((await Read(first.Id)).PaidAmount == 0 && (await Read(second.Id)).PaidAmount == 150 && (await Read(third.Id)).PaidAmount == 250 &&
    account.Debt == 500 && account.Credit == 100 && account.Balance == -400, "Grouped receipt pays only selected amounts and leaves excess as independent wallet credit");
Check(groupReceipt.Allocations.Count == 2 && groupReceipt.Method == "Cash" && groupReceipt.Description == group.Description &&
    groupReceipt.OccurredAt == group.OccurredAt && groupReceipt.RecordedBy == admin, "Receipt history includes selected orders, method, tracking description, actor and occurrence date");
await Reject(() => finance.RecordPayment(debtor, admin, group with { Allocations = [new(first.Id, 200), new(third.Id, 200)] }, default), "Receipt retries must preserve selected allocations");
await RejectInput(() => finance.RecordPayment(debtor, admin, group with { Reference = "bad-duplicate", Allocations = [new(first.Id, 100), new(first.Id, 100)] }, default), "Duplicate order selections are rejected");
await RejectInput(() => finance.RecordPayment(debtor, admin, group with { Reference = "bad-total", Amount = 1 }, default), "Allocation totals cannot exceed receipt amount");
await Reject(() => finance.RecordPayment(debtor, admin, group with { Reference = "bad-overpay", Allocations = [new(first.Id, 201)] }, default), "Allocations cannot overpay an order");
await Reject(() => finance.RecordPayment(debtor, admin, group with { Reference = "bad-owner", Allocations = [new(collisionOrder.Id, 1)] }, default), "Allocations enforce customer ownership");
Check(!await db.FinancialEntries.AnyAsync(x => x.UserId == debtor && x.Reference.StartsWith("bad-")), "Invalid receipt groups leave no ledger postings");
await payments.PayWithWallet(debtor, first.Id, default);
Check((await Read(first.Id)).PaidAmount == 100 && (await finance.GetAccount(debtor)).Credit == 0 && (await finance.GetAccount(debtor)).Debt == 400,
    "Wallet credit can explicitly pay a selected debt order");
await finance.SetTerms(debtor, new(5000, null, AllowPayLater: false), default);
await Reject(async () => await Place(debtor, later: true), "Revoking credit blocks new credit orders");
// Clear the cart left by the rejected attempt, then test that debt remains payable.
var cart = await db.Carts.Include(x => x.Items).SingleAsync(x => x.UserId == debtor); db.CartItems.RemoveRange(cart.Items); await db.SaveChangesAsync();
var debtAttempt = await payments.Start(debtor, third.Id, "test", default); gateway.Succeed(debtAttempt.AttemptId);
await payments.Verify(debtAttempt.AttemptId, default); await payments.Verify(debtAttempt.AttemptId, default);
Check((await Read(third.Id)).PaidAmount == 400 && (await finance.GetAccount(debtor)).Debt == 250, "Gateway settles a debt order after credit permission is revoked, exactly once");
await Status(second.Id, OrderStatus.Cancelled, "Customer withdrew order");
Check((await finance.GetAccount(debtor)).Credit == 150 && (await finance.GetAccount(debtor)).Debt == 100 && (await Read(first.Id)).PaidAmount == 100,
    "Cancellation releases funded money without silently paying unrelated debt");
await Refund(debtor, 150, second.Id);
Check((await finance.GetAccount(debtor)).Credit == 0 && (await finance.GetAccount(debtor)).Debt == 100, "Cancelled-order refund cannot erase unrelated debt");
await Reject(() => Refund(debtor, 1), "Wallet refund cannot exceed available credit");

var liveUser = await Customer(); await Fund(liveUser, 500); var live = await Place(liveUser);
await Status(live.Id, OrderStatus.Accepted); await Status(live.Id, OrderStatus.Shipped); await Status(live.Id, OrderStatus.Delivered);
await Refund(liveUser, 200, live.Id);
Check((await Read(live.Id)).Status == OrderStatus.Delivered && (await Read(live.Id)).RefundedAmount == 200 &&
    (await finance.GetAccount(liveUser)).Balance == 0 && (await finance.GetAccount(liveUser)).Debt == 0, "Live-order partial refund records price credit and payout without reopening debt or fulfilment");
await Refund(liveUser, 300, live.Id);
Check((await Read(live.Id)).PaymentStatus == PaymentStatus.Refunded, "Delivered order supports full refund without changing fulfilment");
await Reject(() => Status(live.Id, OrderStatus.Cancelled), "Delivered orders cannot be cancelled");
await Reject(() => Status(live.Id, OrderStatus.Accepted), "Order statuses cannot move backward");
var liveCancelUser = await Customer(); await Fund(liveCancelUser, 500); var liveCancel = await Place(liveCancelUser);
await Refund(liveCancelUser, 200, liveCancel.Id); await repository.Cancel(liveCancel.Id, liveCancelUser, "Changed mind");
Check((await finance.GetAccount(liveCancelUser)).Credit == 300, "Cancellation after a partial live refund releases only the unrefunded money");
await Refund(liveCancelUser, 300, liveCancel.Id);
Check((await finance.GetAccount(liveCancelUser)).Balance == 0, "Refund then cancellation then final payout preserves ledger balance");

var walletUser = await Customer();
var topUp = await payments.StartWalletTopUp(walletUser, new(500, "test"), default);
Check(!await db.Orders.AnyAsync(x => x.UserId == walletUser), "Gateway top-up starts without creating an order");
gateway.Succeed(topUp.AttemptId); await payments.Verify(topUp.AttemptId, default); await payments.Verify(topUp.AttemptId, default);
Check((await finance.GetAccount(walletUser)).Credit == 500, "Verified pre-order gateway top-up credits wallet once");
var walletOrder = await Place(walletUser);
Check(walletOrder.PaidAmount == 500, "Verified pre-order gateway funds can create a fully prepaid order");
var failedUser = await Customer();
var failed = await payments.StartWalletTopUp(failedUser, new(500, "test"), default); await payments.Verify(failed.AttemptId, default);
Check((await finance.GetAccount(failedUser)).Credit == 0, "Failed gateway top-up cannot fund checkout");
var mismatch = await payments.StartWalletTopUp(failedUser, new(500, "test"), default); gateway.Succeed(mismatch.AttemptId, wrongAmount: true);
await Reject(() => payments.Verify(mismatch.AttemptId, default), "Mismatched gateway amount cannot credit account");
await RejectInput(() => payments.StartWalletTopUp(failedUser, new(0, "test"), default), "Zero top-ups are rejected");
var lateUser = await Customer(); await Enable(lateUser); var late = await Place(lateUser, later: true);
var lateAttempt = await payments.Start(lateUser, late.Id, "test", default);
await repository.Cancel(late.Id, lateUser, "No longer needed"); gateway.Succeed(lateAttempt.AttemptId);
await payments.Verify(lateAttempt.AttemptId, default); await payments.Verify(lateAttempt.AttemptId, default);
Check((await finance.GetAccount(lateUser)).Credit == 500 && (await Read(late.Id)).PaidAmount == 0 && (await Read(late.Id)).Status == OrderStatus.Cancelled,
    "Late gateway success credits wallet once without paying or reviving cancelled order");
var duplicateUser = await Customer(); await Enable(duplicateUser); var duplicateOrder = await Place(duplicateUser, later: true);
var a = await payments.Start(duplicateUser, duplicateOrder.Id, "test", default); var b = await payments.Start(duplicateUser, duplicateOrder.Id, "test", default);
gateway.Succeed(a.AttemptId); gateway.Succeed(b.AttemptId); await payments.Verify(a.AttemptId, default); await payments.Verify(b.AttemptId, default);
Check((await Read(duplicateOrder.Id)).PaidAmount == 500 && (await finance.GetAccount(duplicateUser)).Credit == 500, "Two successful attempts pay an order once and retain excess wallet credit");
var overdueUser = await Customer(); await Enable(overdueUser, due: DateTimeOffset.UtcNow.AddDays(-1)); var overdue = await Place(overdueUser, later: true);
await Status(overdue.Id, OrderStatus.Accepted);
Check((await finance.GetAccount(overdueUser)).OrderingBlockReason!.StartsWith("OVERDUE_DEBT"), "Overdue debt blocks new credit ordering");
await Reject(() => finance.EnsureCanOrder(overdueUser, 1), "Overdue credit checks are enforced");
await Reject(() => finance.GetAccount(admin), "Admins have no customer wallet");
await Reject(() => finance.SetTerms(admin, new(1000, null, AllowPayLater: true), default), "Admins cannot receive customer payment terms");
db.UserRoles.Add(new() { UserId = admin, RoleId = customerRole }); await db.SaveChangesAsync();
await Reject(() => finance.GetAccount(admin), "Admin exclusion holds with both roles");
await Reject(() => payments.PayWithWallet(user, duplicateOrder.Id, default), "Wallet payments enforce order ownership");
await Reject(() => payments.Start(user, duplicateOrder.Id, "test", default), "Gateway initiation enforces order ownership");
await Reject(() => Refund(user, 1, duplicateOrder.Id), "Refunds enforce customer ownership");
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
