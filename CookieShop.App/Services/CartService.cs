using CookieShop.App.DTOs.Cart;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Services;

public class CartService(
    ICartRepository cartRepository,
    IProductRepository productRepository)
{
    public async Task<CartResponse> GetCart(Guid userId)
    {
        var cart = await cartRepository.GetByUserId(userId);
        if (cart is null) return new CartResponse(Guid.Empty, [], 0);
        return ToCartResponse(cart);
    }

    public async Task<CartResponse> AddItem(Guid userId, AddCartItemRequest request)
    {
        await ValidateProduct(request.ProductId, request.Quantity);
        await cartRepository.AddItem(userId, request);
        return await GetCart(userId);
    }

    public async Task<CartResponse> UpdateItemQuantity(Guid userId, UpdateCartItemRequest request)
    {
        await ValidateProduct(request.ProductId, request.Quantity);
        var result = await cartRepository.UpdateItemQuantity(userId, request);
        if (result is null) throw new NotFoundException("Cart item not found");
        return await GetCart(userId);
    }

    public async Task<CartResponse> RemoveItem(Guid userId, Guid productId)
    {
        if (!await cartRepository.RemoveItem(userId, productId))
            throw new NotFoundException("Cart item not found");
        return await GetCart(userId);
    }

    public async Task<CartResponse> Clear(Guid userId)
    {
        await cartRepository.Clear(userId);
        return await GetCart(userId);
    }

    private async Task ValidateProduct(Guid productId, int quantity)
    {
        var product = await productRepository.GetById(productId);
        if (product is null) throw new NotFoundException("Product not found");

        if (!product.IsAvailable)
            throw new ConflictException($"Product \"{product.Name}\" is not available.");

        if (product.StockQuantity < quantity)
            throw new ConflictException(
                $"Insufficient stock for product \"{product.Name}\" (available: {product.StockQuantity}).");
    }

    private static CartResponse ToCartResponse(Cart cart)
    {
        var items = cart.Items.Select(ToCartItemResponse).ToList();
        return new CartResponse(cart.Id, items, items.Sum(i => i.LineTotal));
    }

    private static CartItemResponse ToCartItemResponse(CartItem item)
    {
        var unitPrice = item.Product.IsOnSale && item.Product.SalePrice is { } sale ? sale : item.Product.Price;
        return new CartItemResponse(
            item.ProductId,
            item.Product.Name,
            item.Product.ImageUrl,
            item.Product.Price,
            unitPrice,
            item.Quantity,
            unitPrice * item.Quantity);
    }
}
