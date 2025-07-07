using ECommerce.Core.Entities;
using ECommerce.Core.Enums;

namespace ECommerce.Core.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId);
    Task<IEnumerable<Order>> GetPendingOrdersAsync();
    Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
}