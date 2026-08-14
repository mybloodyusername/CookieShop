using System.ComponentModel.DataAnnotations;
using CookieShop.App.DTOs.Address;
using CookieShop.App.DTOs.Common;
using CookieShop.App.DTOs.Order;
using CookieShop.App.Exceptions;
using CookieShop.App.Interfaces.Repositories;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CookieShop.App.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IAddressRepository addressRepository,
    ILogger<OrderService> logger)
{
    public async Task<OrderDetailResponse> PlaceOrder(Guid userId, CreateOrderRequest request)
    {
        var address = await addressRepository.GetById(request.AddressId);
        if (address is null) throw new NotFoundException("Address not found.");
        if (address.UserId != userId)
            throw new UnauthorizedAccessException("Address does not belong to the current user.");

        try
        {
            var order = await orderRepository.PlaceOrder(userId, request.AddressId, request.Note);
            var detail = await orderRepository.GetById(order.Id);
            if (detail is null) throw new NotFoundException("Order not found");
            return ToDetail(detail);
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to place order for user {UserId}", userId);
            throw new ConflictException("Failed to place order");
        }
    }

    public async Task<IReadOnlyCollection<OrderResponse>> GetMyOrders(Guid userId)
    {
        var result = await orderRepository.GetByUserId(userId);
        return result.Select(ToResponse).ToList();
    }

    public async Task<OrderDetailResponse> GetById(Guid userId, Guid id)
    {
        var order = await orderRepository.GetById(id);
        if (order is null) throw new NotFoundException("Order not found");
        if (order.UserId != userId) throw new UnauthorizedAccessException();
        return ToDetail(order);
    }

    public async Task<OrderDetailResponse> GetByIdByAdmin(Guid id)
    {
        var order = await orderRepository.GetById(id);
        if (order is null) throw new NotFoundException("Order not found");
        return ToDetail(order);
    }

    public async Task<PagedResult<OrderResponse>> GetAllByAdmin(OrderStatus? status, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;

        var result = await orderRepository.GetByAdmin(status, page, pageSize);
        return new PagedResult<OrderResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }

    public async Task<OrderResponse> Cancel(Guid userId, Guid orderId)
    {
        var order = await orderRepository.GetById(orderId);
        if (order is null) throw new NotFoundException("Order not found");
        if (order.UserId != userId) throw new UnauthorizedAccessException();
        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("Only pending orders can be cancelled.");

        var cancelled = await orderRepository.Cancel(orderId)
                        ?? throw new NotFoundException("Order not found");
        return ToResponse(cancelled);
    }

    public async Task<OrderResponse> UpdateStatusByAdmin(UpdateOrderStatusRequest request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var newStatus) ||
            !Enum.IsDefined(newStatus))
            throw new ValidationException("Invalid order status.");

        var order = await orderRepository.GetById(request.OrderId);
        if (order is null) throw new NotFoundException("Order not found");

        var allowed = (order.Status, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
        if (!allowed)
            throw new ConflictException($"Invalid status transition from {order.Status} to {newStatus}.");

        var updated = await orderRepository.UpdateStatus(order.Id, newStatus)
                      ?? throw new NotFoundException("Order not found");
        return ToResponse(updated);
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.UserId,
        order.Status.ToString(),
        order.TotalAmount,
        order.Note,
        order.CreatedAt,
        order.UpdatedAt);

    private static OrderDetailResponse ToDetail(Order order) => new(
        order.Id,
        order.UserId,
        order.Status.ToString(),
        order.TotalAmount,
        order.Note,
        order.CreatedAt,
        order.UpdatedAt,
        order.Address.Adapt<AddressResponse>(),
        order.Items.Select(ToItemResponse).ToList());

    private static OrderItemResponse ToItemResponse(OrderItem item) => new(
        item.ProductId,
        item.ProductName,
        item.UnitPrice,
        item.Quantity,
        item.UnitPrice * item.Quantity);
}
