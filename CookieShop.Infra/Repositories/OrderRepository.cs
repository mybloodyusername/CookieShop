using System.Data;
using System.ComponentModel.DataAnnotations;
using Npgsql;
using CookieShop.App.DTOs.Common;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Constants;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using CookieShop.Infra.Data;
using CookieShop.Infra.Services;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class OrderRepository(CookieShopDbContext context, CustomerFinanceService finance)
    : IOrderRepository
{
    public async Task<Order?> GetById(Guid id)
    {
        return await context.Orders.AsNoTracking().AsSplitQuery()
            .Include(o => o.Items)
            .Include(o => o.StatusChanges)
            .Include(o => o.Address)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IReadOnlyCollection<Order>> GetByUserId(Guid userId)
    {
        return await context.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedResult<Order>> GetByAdmin(OrderStatus? status, int page, int pageSize)
    {
        var query = context.Orders.AsNoTracking();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Order>(items, totalCount, page, pageSize);
    }

    public async Task<Order> PlaceOrder(Guid userId, Guid addressId, string? note, bool payLater = false, bool allowUnfunded = false)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await PlaceOrderOnce(userId, addressId, note, payLater, allowUnfunded);
            }
            catch (Exception e) when (attempt < 3 &&
                (e is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } ||
                 e is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } }))
            {
                // A concurrent insert can surface as a serialization failure rather
                // than a unique violation. Restart from a fresh snapshot after rollback.
                context.ChangeTracker.Clear();
            }
        }
    }

    private async Task<Order> PlaceOrderOnce(Guid userId, Guid addressId, string? note, bool payLater, bool allowUnfunded)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var address = await context.Addresses.AsNoTracking()
                          .FirstOrDefaultAsync(x => x.Id == addressId && x.UserId == userId)
                      ?? throw new NotFoundException("Address not found.");
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
            throw new ConflictException("Cart is empty.");

        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var total = 0m;
        var orderItems = new List<OrderItem>();

        foreach (var item in cart.Items)
        {
            if (item.Quantity < 1) throw new ConflictException("Invalid cart quantity.");
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null)
                throw new ConflictException("A product in your cart is no longer available.");

            if (!product.IsAvailable)
                throw new ConflictException($"Product \"{product.Name}\" is not available.");

            if (product.StockQuantity < item.Quantity)
                throw new ConflictException(
                    $"Insufficient stock for product \"{product.Name}\" (available: {product.StockQuantity}).");

            var unitPrice = product.IsOnSale && product.SalePrice is { } sale ? sale : product.Price;
            total += unitPrice * item.Quantity;
            if (!StoreCurrency.IsValid(total))
                throw new ConflictException("Order total exceeds the supported toman amount.");

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
            });

            product.StockQuantity -= item.Quantity;
        }

        if (!allowUnfunded)
        {
            var account = await finance.GetAccount(userId);
            if (!payLater && account.Credit < total)
                throw new ConflictException("PAYMENT_REQUIRED: Fund your wallet before creating this order.");
        }

        var pendingExposure = await context.Orders.Where(x => x.UserId == userId && x.Status == OrderStatus.Pending && x.PayLater)
            .SumAsync(x => (decimal?)(x.TotalAmount - x.PaidAmount)) ?? 0;
        if (payLater) await finance.EnsureCanOrder(userId, pendingExposure + total);

        var order = new Order
        {
            UserId = userId,
            AddressId = addressId,
            ShippingTitle = address.Title,
            ShippingAddressLine1 = address.AddressLine1,
            ShippingAddressLine2 = address.AddressLine2,
            ShippingPhoneNumber = address.PhoneNumber,
            Status = OrderStatus.Pending,
            PayLater = payLater,
            TotalAmount = total,
            Note = note,
            Items = orderItems,
        };
        order.StatusChanges.Add(new() { ToStatus = OrderStatus.Pending, ActorId = userId, Reason = "Order created" });
        order.OrderNumber = OrderNumberGenerator.Generate(order.CreatedAt);
        await context.Orders.AddAsync(order);

        await finance.PayFromWallet(order);
        CustomerFinanceService.RefreshPaymentStatus(order);
        context.CartItems.RemoveRange(cart.Items);

        // EF rolls failed saves back to its transaction savepoint, including stock,
        // wallet and cart changes. Retry only the order-number unique violation.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await context.SaveChangesAsync();
                break;
            }
            catch (DbUpdateException e) when (attempt < 20 &&
                e.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Orders_OrderNumber" })
            {
                order.OrderNumber = OrderNumberGenerator.Generate(order.CreatedAt);
            }
        }
        await transaction.CommitAsync();
        return order;
    }

    public async Task<Order?> Cancel(Guid id, Guid actorId, string reason) =>
        await CancelOrReject(id, OrderStatus.Cancelled, actorId, reason, allowAccepted: false);

    private async Task<Order?> CancelOrReject(Guid id, OrderStatus terminalStatus, Guid actorId, string? reason, bool allowAccepted = true)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 512)
            throw new ValidationException("A cancellation/rejection reason of at most 512 characters is required.");
        await using var transaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return null;
        if (order.Status == terminalStatus) return order;
        if (terminalStatus == OrderStatus.Rejected && order.Status != OrderStatus.Pending)
            throw new ConflictException("Only pending orders can be rejected.");
        if (order.Status != OrderStatus.Pending && !(allowAccepted && order.Status == OrderStatus.Accepted))
            throw new ConflictException("Only pending orders can be cancelled.");

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is not null)
                product.StockQuantity += item.Quantity;
        }

        // Reverse exactly what was charged: funded money for pending orders, full charge for accepted ones.
        var charged = -(await context.FinancialEntries.Where(x => x.OrderId == id &&
            (x.Kind == "OrderCharge" || x.Kind == "OrderPrepayment")).SumAsync(x => (decimal?)x.Amount) ?? 0);
        charged -= order.RefundedAmount;
        if (charged > 0)
        {
            var credit = new FinancialEntry { UserId = order.UserId, OrderId = order.Id,
                Amount = charged, Kind = "CancellationCredit" };
            context.FinancialEntries.Add(credit);
        }
        context.OrderStatusChanges.Add(new() { OrderId = order.Id, FromStatus = order.Status, ToStatus = terminalStatus, ActorId = actorId, Reason = reason.Trim() });
        order.Status = terminalStatus;
        CustomerFinanceService.RefreshPaymentStatus(order);
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return order;
    }

    public async Task<Order?> UpdateStatus(Guid id, OrderStatus newStatus, Guid actorId, string? reason = null)
    {
        if (newStatus is OrderStatus.Cancelled or OrderStatus.Rejected) return await CancelOrReject(id, newStatus, actorId, reason);
        await using var transaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return null;

        var allowed = (order.Status, newStatus) is
            (OrderStatus.Pending, OrderStatus.Accepted) or
            (OrderStatus.Accepted, OrderStatus.Shipped) or
            (OrderStatus.Shipped, OrderStatus.Delivered);
        if (!allowed) throw new ConflictException("Invalid order status transition.");
        if (newStatus == OrderStatus.Accepted)
        {
            await finance.PayFromWallet(order);
            await context.SaveChangesAsync();
            var outstanding = order.TotalAmount - order.PaidAmount;
            if (outstanding > 0)
            {
                if (!order.PayLater) throw new ConflictException("Payment is required before accepting this order.");
                var reserved = await context.Orders.Where(x => x.UserId == order.UserId && x.Id != order.Id && x.PayLater && x.Status == OrderStatus.Pending)
                    .SumAsync(x => (decimal?)(x.TotalAmount - x.PaidAmount)) ?? 0;
                await finance.EnsureCanOrder(order.UserId, reserved + outstanding);
            }
            if (outstanding > 0)
                context.FinancialEntries.Add(new FinancialEntry
                {
                    UserId = order.UserId, OrderId = order.Id, Amount = -outstanding, Kind = "OrderCharge",
                    DueAt = await finance.ChargeDueAt(order.UserId)
                });
        }

        if (reason?.Length > 512) throw new ValidationException("Status reason must be at most 512 characters.");
        context.OrderStatusChanges.Add(new() { OrderId = order.Id, FromStatus = order.Status, ToStatus = newStatus, ActorId = actorId, Reason = reason?.Trim() ?? "" });
        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return order;
    }
}