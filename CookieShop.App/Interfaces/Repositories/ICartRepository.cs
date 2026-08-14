using CookieShop.App.DTOs.Cart;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Interfaces.Repositories;

public interface ICartRepository
{
    public Task<Cart?> GetByUserId(Guid userId);
    public Task<Cart> AddItem(Guid userId, AddCartItemRequest request);
    public Task<Cart?> UpdateItemQuantity(Guid userId, UpdateCartItemRequest request);
    public Task<bool> RemoveItem(Guid userId, Guid productId);
    public Task Clear(Guid userId);
}
