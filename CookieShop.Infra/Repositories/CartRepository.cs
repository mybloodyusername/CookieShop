using CookieShop.App.DTOs.Cart;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace CookieShop.Infra.Repositories;

public class CartRepository(CookieShopDbContext context) : ICartRepository
{
    public async Task<Cart?> GetByUserId(Guid userId)
    {
        return await context.Carts.AsNoTracking()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart> AddItem(Guid userId, AddCartItemRequest request)
    {
        var cart = await GetOrCreate(userId);

        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item is null)
            cart.Items.Add(new CartItem { ProductId = request.ProductId, Quantity = request.Quantity });
        else
            item.Quantity += request.Quantity;

        await context.SaveChangesAsync();
        return cart;
    }

    public async Task<Cart?> UpdateItemQuantity(Guid userId, UpdateCartItemRequest request)
    {
        var cart = await GetOrCreate(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item is null) return null;

        item.Quantity = request.Quantity;

        await context.SaveChangesAsync();
        return cart;
    }

    public async Task<bool> RemoveItem(Guid userId, Guid productId)
    {
        var cart = await GetOrCreate(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return false;

        cart.Items.Remove(item);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task Clear(Guid userId)
    {
        var cart = await GetOrCreate(userId);
        if (cart.Items.Count == 0) return;

        context.CartItems.RemoveRange(cart.Items);
        await context.SaveChangesAsync();
    }

    private async Task<Cart> GetOrCreate(Guid userId)
    {
        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart is not null) return cart;

        cart = new Cart { UserId = userId };
        await context.Carts.AddAsync(cart);
        return cart;
    }
}
