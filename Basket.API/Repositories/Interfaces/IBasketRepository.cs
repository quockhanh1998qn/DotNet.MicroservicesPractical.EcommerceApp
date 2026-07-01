using Basket.API.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Basket.API.Repositories.Interfaces;

public interface IBasketRepository
{
	Task<Cart?> GetBasketAsync(string username, CancellationToken cancellationToken = default);
	Task<Cart> UpdateBasketAsync(Cart cart, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
	Task DeleteBasketAsync(string username, CancellationToken cancellationToken = default);
}
