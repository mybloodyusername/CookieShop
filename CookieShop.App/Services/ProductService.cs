using System.ComponentModel.DataAnnotations;
using CookieShop.Domain.Constants;
using CookieShop.App.DTOs.Media;
using CookieShop.Domain.Entities;
using CookieShop.App.DTOs.Common;
using CookieShop.App.DTOs.Product;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CookieShop.App.Services;

public class ProductService(IProductRepository productRepository, ILogger<ProductService> logger, MediaUrlProvider urls)
{
    public async Task<ProductDetailResponse> GetById(Guid id)
    {
        var result = await productRepository.GetById(id);
        if (result is null) throw new NotFoundException("Product not found");
        var response = result.Adapt<ProductDetailResponse>();
        return response with { Images = Images(result), ImageUrl = Images(result).FirstOrDefault()?.Url ?? result.ImageUrl };
    }

    public async Task<PagedResult<ProductResponse>> GetAll(Guid? categoryId, string? search, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var result = await productRepository.GetFiltered(categoryId, search, page, pageSize);
        return new PagedResult<ProductResponse>(
            result.Items.Select(x => x.Adapt<ProductResponse>() with { Images = Images(x), ImageUrl = Images(x).FirstOrDefault()?.Url ?? x.ImageUrl }).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    public async Task<ProductResponse> Create(CreateProductRequest request)
    {
        Validate(request);
        try
        {
            var result = await productRepository.Create(request);
            return result.Adapt<ProductResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to create product {Name}", request.Name);
            throw new ConflictException("Failed to create product");
        }
    }

    public async Task<ProductResponse> Update(UpdateProductRequest request)
    {
        Validate(request);
        try
        {
            var result = await productRepository.Update(request);
            if (result is null) throw new NotFoundException("Product not found");
            return result.Adapt<ProductResponse>();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to update product with {ProductId}", request.Id);
            throw new ConflictException("Failed to update product");
        }
    }

    public async Task<bool> Delete(Guid id)
    {
        try
        {
            if (!await productRepository.Delete(id))
                throw new NotFoundException("Product not found.");
            return true;
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to delete product with {ProductId}", id);
            throw new ConflictException("Product cannot be deleted because orders reference it.");
        }
    }

    private IReadOnlyCollection<ProductImageResponse> Images(Product product) => product.Images
        .OrderBy(x => x.SortOrder).ThenBy(x => x.ImageId)
        .Select(x => new ProductImageResponse(x.ImageId, urls.Get(x.ImageId), x.Alt ?? x.Image.Alt, x.SortOrder)).ToList();

    private static void Validate(CreateProductRequest request)
    {
        if (!StoreCurrency.IsValid(request.Price) || request.Price < 1 ||
            (request.SalePrice is { } sale && (!StoreCurrency.IsValid(sale) || sale < 1 || sale >= request.Price)))
            throw new ValidationException("Prices must be whole toman; sale price must be positive and below regular price.");
        if (request.CategoryId == Guid.Empty) throw new ValidationException("Category is required.");
        if (request.IsOnSale && request.SalePrice is null)
            throw new ValidationException("SalePrice is required when IsOnSale is true.");
        if (request.StockQuantity < 0)
            throw new ValidationException("StockQuantity cannot be negative.");
    }

    private static void Validate(UpdateProductRequest request)
    {
        if (!StoreCurrency.IsValid(request.Price) || request.Price < 1 ||
            (request.SalePrice is { } sale && (!StoreCurrency.IsValid(sale) || sale < 1 || sale >= request.Price)))
            throw new ValidationException("Prices must be whole toman; sale price must be positive and below regular price.");
        if (request.CategoryId == Guid.Empty) throw new ValidationException("Category is required.");
        if (request.IsOnSale && request.SalePrice is null)
            throw new ValidationException("SalePrice is required when IsOnSale is true.");
        if (request.StockQuantity < 0)
            throw new ValidationException("StockQuantity cannot be negative.");
    }
}
