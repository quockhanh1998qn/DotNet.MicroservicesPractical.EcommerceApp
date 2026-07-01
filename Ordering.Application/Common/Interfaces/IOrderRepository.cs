using Ordering.Domain.Entities;

namespace Ordering.Application.Common.Interfaces;

public interface IOrderRepository
{
	Task<IReadOnlyList<Order>> GetOrdersByUserNameAsync(string userName, CancellationToken cancellationToken = default);
	Task<Order?> GetOrderByIdAsync(long id, CancellationToken cancellationToken = default);
	Task<long> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);
	Task UpdateOrderAsync(Order order, CancellationToken cancellationToken = default);
	Task DeleteOrderAsync(Order order, CancellationToken cancellationToken = default);
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
