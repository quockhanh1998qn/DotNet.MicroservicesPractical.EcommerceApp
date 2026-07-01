using AutoMapper;
using Basket.API;
using Basket.API.Controllers;
using Basket.API.Entities;
using Basket.API.Repositories;
using Basket.API.Repositories.Interfaces;
using Basket.API.Services;
using EventBus.Messages.IntegrationEvents.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Shared.DTOs.Basket;

namespace Basket.API.UnitTests;

public class CheckoutTests
{
	private static readonly IMapper Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

	[Fact]
	public async Task Checkout_PublishesEventAndDeletesBasket()
	{
		var (controller, repository, publisher) = BuildController();

		await repository.UpdateBasketAsync(new Cart("swn")
		{
			Items = new List<CartItem>
			{
				new() { ProductNo = "P-001", ProductName = "X", Quantity = 2, Price = 25m }
			}
		});

		var result = await controller.Checkout(new BasketCheckoutDto
		{
			UserName = "swn",
			FirstName = "T",
			LastName = "U",
			EmailAddress = "a@b.com",
			ShippingAddress = "ship",
			InvoiceAddress = "inv",
			TotalPrice = 0
		}, CancellationToken.None);

		Assert.IsType<AcceptedResult>(result);
		Assert.Single(publisher.Published);
		Assert.Equal(50m, publisher.Published[0].TotalPrice);
		Assert.Null(await repository.GetBasketAsync("swn"));
	}

	[Fact]
	public async Task Checkout_ReturnsNotFound_WhenBasketMissing()
	{
		var (controller, _, publisher) = BuildController();

		var result = await controller.Checkout(new BasketCheckoutDto
		{
			UserName = "ghost",
			FirstName = "T",
			LastName = "U",
			EmailAddress = "a@b.com",
			ShippingAddress = "ship",
			InvoiceAddress = "inv"
		}, CancellationToken.None);

		Assert.IsType<NotFoundObjectResult>(result);
		Assert.Empty(publisher.Published);
	}

	private static (BasketsController controller, BasketRepository repository, RecordingPublishEndpoint publisher) BuildController()
	{
		var repository = new BasketRepository(new InMemoryDistributedCache());
		var publisher = new RecordingPublishEndpoint();
		var stockService = new InfiniteStockService();
		var controller = new BasketsController(repository, Mapper, publisher, stockService);
		return (controller, repository, publisher);
	}

	private sealed class InfiniteStockService : IStockService
	{
		public Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default) => Task.FromResult(int.MaxValue);
	}

	private sealed class RecordingPublishEndpoint : IPublishEndpoint
	{
		public List<BasketCheckoutEvent> Published { get; } = new();

		public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
		{
			if (message is BasketCheckoutEvent evt)
			{
				Published.Add(evt);
			}
			return Task.CompletedTask;
		}

		public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => Publish(message, cancellationToken);
		public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => Publish(message, cancellationToken);
		public Task Publish(object message, CancellationToken cancellationToken = default) => Publish((BasketCheckoutEvent)message, cancellationToken);
		public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) => Publish(message, cancellationToken);
		public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => Publish(message, cancellationToken);
		public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) => Publish(message, cancellationToken);
		public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
		public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
		public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
		public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();
	}

	private sealed class InMemoryDistributedCache : IDistributedCache
	{
		private readonly Dictionary<string, byte[]> _store = new();
		public byte[]? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;
		public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
		public void Refresh(string key) { }
		public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
		public void Remove(string key) => _store.Remove(key);
		public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
		public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;
		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
	}
}
