using CookieShop.App.DTOs.Common;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class OrderRepository(CookieShopDbContext context) : IOrderRepository
{
    public async Task<Order?> GetById(Guid id)
    {
        return await context.Orders.AsNoTracking()
            .Include(o => o.Items)
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

    public async Task<Order> PlaceOrder(Guid userId, Guid addressId, string? note)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
            throw new ConflictException("Cart is empty.");

        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var total = 0L;
        var orderItems = new List<OrderItem>();

        foreach (var item in cart.Items)
        {
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

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
            });

            product.StockQuantity -= item.Quantity;
        }

        var order = new Order
        {
            UserId = userId,
            AddressId = addressId,
            Status = OrderStatus.Pending,
            TotalAmount = total,
            Note = note,
            Items = orderItems,
        };
        await context.Orders.AddAsync(order);

        context.CartItems.RemoveRange(cart.Items);

        await context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> Cancel(Guid id)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return null;

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

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> UpdateStatus(Guid id, OrderStatus newStatus)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return null;

        order.Status = newStatus;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();
        return order;
    }
}
