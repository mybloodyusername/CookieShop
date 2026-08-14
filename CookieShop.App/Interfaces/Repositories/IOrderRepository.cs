using CookieShop.App.DTOs.Common;
using CookieShop.Domain.Entities;
using CookieShop.Domain.Enums;

namespace CookieShop.App.Interfaces.Repositories;

public interface IOrderRepository
{
    public Task<Order?> GetById(Guid id);
    public Task<IReadOnlyCollection<Order>> GetByUserId(Guid userId);
    public Task<PagedResult<Order>> GetByAdmin(OrderStatus? status, int page, int pageSize);
    public Task<Order> PlaceOrder(Guid userId, Guid addressId, string? note);
    public Task<Order?> Cancel(Guid id);
    public Task<Order?> UpdateStatus(Guid id, OrderStatus newStatus);
}
