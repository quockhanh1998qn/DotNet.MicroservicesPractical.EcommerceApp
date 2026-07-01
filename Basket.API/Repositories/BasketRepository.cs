using System.Text.Json;
using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Basket.API.Repositories;

public class BasketRepository : IBasketRepository
{
	private readonly IDistributedCache _cache;
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public BasketRepository(IDistributedCache cache)
	{
		_cache = cache;
	}

	public async Task<Cart?> GetBasketAsync(string username, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(username);
		var payload = await _cache.GetStringAsync(BuildKey(username), cancellationToken);
		return string.IsNullOrEmpty(payload) ? null : JsonSerializer.Deserialize<Cart>(payload, JsonOptions);
	}

	public async Task<Cart> UpdateBasketAsync(Cart cart, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(cart);
		ArgumentException.ThrowIfNullOrWhiteSpace(cart.Username);

		var payload = JsonSerializer.Serialize(cart, JsonOptions);
		var cacheOptions = options ?? new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
			SlidingExpiration = TimeSpan.FromHours(2)
		};

		await _cache.SetStringAsync(BuildKey(cart.Username), payload, cacheOptions, cancellationToken);
		return cart;
	}

	public Task DeleteBasketAsync(string username, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(username);
		return _cache.RemoveAsync(BuildKey(username), cancellationToken);
	}

	private static string BuildKey(string username) => $"basket:{username.Trim().ToLowerInvariant()}";
}
