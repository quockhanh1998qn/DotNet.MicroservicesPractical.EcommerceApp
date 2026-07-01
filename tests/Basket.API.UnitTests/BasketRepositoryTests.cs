using System.Text;
using System.Text.Json;
using Basket.API.Entities;
using Basket.API.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace Basket.API.UnitTests;

public class BasketRepositoryTests
{
	[Fact]
	public async Task UpdateBasket_ThenGet_ReturnsPersistedCart()
	{
		var cache = new InMemoryDistributedCache();
		var repository = new BasketRepository(cache);

		var cart = new Cart("swn")
		{
			Items = new List<CartItem>
			{
				new() { ProductNo = "P-001", ProductName = "Foo", Quantity = 2, Price = 10m }
			}
		};

		await repository.UpdateBasketAsync(cart);
		var fetched = await repository.GetBasketAsync("swn");

		Assert.NotNull(fetched);
		Assert.Equal("swn", fetched!.Username);
		Assert.Single(fetched.Items);
		Assert.Equal(20m, fetched.TotalPrice);
	}

	[Fact]
	public async Task GetBasket_WhenMissing_ReturnsNull()
	{
		var cache = new InMemoryDistributedCache();
		var repository = new BasketRepository(cache);

		var result = await repository.GetBasketAsync("ghost");

		Assert.Null(result);
	}

	[Fact]
	public async Task DeleteBasket_RemovesEntry()
	{
		var cache = new InMemoryDistributedCache();
		var repository = new BasketRepository(cache);
		await repository.UpdateBasketAsync(new Cart("swn"));

		await repository.DeleteBasketAsync("swn");
		var result = await repository.GetBasketAsync("swn");

		Assert.Null(result);
	}

	[Fact]
	public async Task GetBasket_IsCaseInsensitiveByUsername()
	{
		var cache = new InMemoryDistributedCache();
		var repository = new BasketRepository(cache);
		await repository.UpdateBasketAsync(new Cart("Swn"));

		var result = await repository.GetBasketAsync("SWN");

		Assert.NotNull(result);
	}

	[Fact]
	public async Task UpdateBasket_NullCart_Throws()
	{
		var repository = new BasketRepository(new InMemoryDistributedCache());

		await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateBasketAsync(null!));
	}

	[Fact]
	public void CartTotalPrice_SumsItems()
	{
		var cart = new Cart("u")
		{
			Items = new List<CartItem>
			{
				new() { Quantity = 2, Price = 5m },
				new() { Quantity = 3, Price = 10m }
			}
		};

		Assert.Equal(40m, cart.TotalPrice);
	}

	private sealed class InMemoryDistributedCache : IDistributedCache
	{
		private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

		public byte[]? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;
		public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
		public void Refresh(string key) { }
		public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
		public void Remove(string key) => _store.Remove(key);
		public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
		public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;
		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{ Set(key, value, options); return Task.CompletedTask; }
	}
}
