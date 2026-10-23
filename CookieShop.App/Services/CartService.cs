using CookieShop.App.DTOs.Cart;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;

namespace CookieShop.App.Services;

public class CartService(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    MediaUrlProvider urls)
{
    public async Task<CartResponse> GetCart(Guid userId)
    {
        var cart = await cartRepository.GetByUserId(userId);
        if (cart is null) return new CartResponse(Guid.Empty, [], 0);
        return ToCartResponse(cart);
    }

    public async Task<CartResponse> AddItem(Guid userId, AddCartItemRequest request)
    {
        var cart = await cartRepository.GetByUserId(userId);
        var existing = cart?.Items.FirstOrDefault(x => x.ProductId == request.ProductId)?.Quantity ?? 0;
        await ValidateProduct(request.ProductId, checked(existing + request.Quantity));
        if (request.Quantity < 1) throw new System.ComponentModel.DataAnnotations.ValidationException("Quantity must be positive.");
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
        if (quantity is < 1 or > 1000) throw new System.ComponentModel.DataAnnotations.ValidationException("Quantity must be between 1 and 1000.");
        var product = await productRepository.GetById(productId);
        if (product is null) throw new NotFoundException("Product not found");

        if (!product.IsAvailable)
            throw new ConflictException($"Product \"{product.Name}\" is not available.");

        if (product.StockQuantity < quantity)
            throw new ConflictException(
                $"Insufficient stock for product \"{product.Name}\" (available: {product.StockQuantity}).");
    }

    private CartResponse ToCartResponse(Cart cart)
    {
        var items = cart.Items.Select(ToCartItemResponse).ToList();
        return new CartResponse(cart.Id, items, items.Sum(i => i.LineTotal));
    }

    private CartItemResponse ToCartItemResponse(CartItem item)
    {
        var unitPrice = item.Product.IsOnSale && item.Product.SalePrice is { } sale ? sale : item.Product.Price;
        return new CartItemResponse(
            item.ProductId,
            item.Product.Name,
            item.Product.Images.OrderBy(x => x.SortOrder).ThenBy(x => x.ImageId).Select(x => urls.Get(x.ImageId)).FirstOrDefault() ?? item.Product.ImageUrl,
            item.Product.Price,
            unitPrice,
            item.Quantity,
            unitPrice * item.Quantity);
    }
}
