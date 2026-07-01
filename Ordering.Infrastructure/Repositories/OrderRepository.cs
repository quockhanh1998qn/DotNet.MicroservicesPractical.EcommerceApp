using Contracts.Common.Interfaces;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Common.Interfaces;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure.Repositories;

public class OrderRepository : RepositoryBaseAsync<Order, long, OrderContext>, IOrderRepository
{
	public OrderRepository(OrderContext dbContext, IUnitOfWork<OrderContext> unitOfWork) : base(dbContext, unitOfWork)
	{
	}

	public async Task<IReadOnlyList<Order>> GetOrdersByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
		await FindByCondition(x => x.UserName == userName).ToListAsync(cancellationToken);

	public Task<Order?> GetOrderByIdAsync(long id, CancellationToken cancellationToken = default) =>
		FindByCondition(x => x.Id == id, trackChanges: true).FirstOrDefaultAsync(cancellationToken);

	public async Task<long> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
	{
		return await CreateAsync(order);
	}

	public Task UpdateOrderAsync(Order order, CancellationToken cancellationToken = default) => UpdateAsync(order);

	public Task DeleteOrderAsync(Order order, CancellationToken cancellationToken = default) => DeleteAsync(order);

	Task<int> IOrderRepository.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync();
}
