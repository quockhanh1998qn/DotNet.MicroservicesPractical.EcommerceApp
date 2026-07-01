using AutoMapper;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Ordering.Application.Features.Orders.Commands.CreateOrder;
using Ordering.Application.Features.Orders.Commands.DeleteOrder;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrderById;
using Ordering.Application.Features.Orders.Queries.GetOrdersByUserName;
using Ordering.Application.Mappings;
using Ordering.Domain.Entities;
using Ordering.Domain.ValueObjects;
using Ordering.Infrastructure.Persistence;
using Ordering.Infrastructure.Repositories;

namespace Ordering.Application.UnitTests;

public class OrderHandlerTests
{
	private static readonly IMapper Mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderingMappingProfile>()).CreateMapper();

	[Fact]
	public void CreateOrderValidator_FailsOnInvalidInput()
	{
		var validator = new CreateOrderCommandValidator();
		var result = validator.Validate(new CreateOrderCommand { TotalPrice = 0 });
		Assert.False(result.IsValid);
	}

	[Fact]
	public void CreateOrderValidator_PassesOnValidInput()
	{
		var validator = new CreateOrderCommandValidator();
		var result = validator.Validate(new CreateOrderCommand
		{
			UserName = "swn",
			EmailAddress = "a@b.com",
			ShippingAddress = "addr",
			TotalPrice = 10
		});
		Assert.True(result.IsValid);
	}

	[Fact]
	public async Task CreateOrderHandler_PersistsOrder_AndReturnsDto()
	{
		await using var context = CreateContext();
		var repo = CreateRepo(context);
		var handler = new CreateOrderCommandHandler(repo, Mapper, NullLogger<CreateOrderCommandHandler>.Instance);

		var dto = await handler.Handle(new CreateOrderCommand
		{
			UserName = "swn",
			FirstName = "T",
			LastName = "U",
			EmailAddress = "a@b.com",
			ShippingAddress = "ship",
			InvoiceAddress = "inv",
			TotalPrice = 99.5m
		}, CancellationToken.None);

		Assert.True(dto.Id > 0);
		Assert.Equal("swn", dto.UserName);
		Assert.Single(context.Orders);
	}

	[Fact]
	public async Task GetOrdersByUserName_ReturnsMatching()
	{
		await using var context = CreateContext();
		context.Orders.AddRange(
			new Order { UserName = "swn", EmailAddress = "a@b.com", TotalPrice = 10 },
			new Order { UserName = "other", EmailAddress = "a@b.com", TotalPrice = 5 });
		await context.SaveChangesAsync();

		var repo = CreateRepo(context);
		var handler = new GetOrdersByUserNameQueryHandler(repo, Mapper);

		var result = await handler.Handle(new GetOrdersByUserNameQuery("swn"), CancellationToken.None);

		Assert.Single(result);
		Assert.Equal("swn", result[0].UserName);
	}

	[Fact]
	public async Task GetOrderById_WhenMissing_ReturnsNull()
	{
		await using var context = CreateContext();
		var handler = new GetOrderByIdQueryHandler(CreateRepo(context), Mapper);

		var result = await handler.Handle(new GetOrderByIdQuery(999), CancellationToken.None);

		Assert.Null(result);
	}

	[Fact]
	public async Task UpdateOrderHandler_UpdatesExistingOrder()
	{
		await using var context = CreateContext();
		var seeded = new Order { UserName = "swn", EmailAddress = "a@b.com", TotalPrice = 10 };
		context.Orders.Add(seeded);
		await context.SaveChangesAsync();
		context.ChangeTracker.Clear();

		var repo = CreateRepo(context);
		var handler = new UpdateOrderCommandHandler(repo, Mapper, NullLogger<UpdateOrderCommandHandler>.Instance);

		var ok = await handler.Handle(new UpdateOrderCommand
		{
			Id = seeded.Id,
			UserName = "swn",
			EmailAddress = "a@b.com",
			TotalPrice = 50,
			Status = (int)OrderStatus.Accepted
		}, CancellationToken.None);

		Assert.True(ok);
		var updated = await context.Orders.FindAsync(seeded.Id);
		Assert.Equal(50m, updated!.TotalPrice);
		Assert.Equal(OrderStatus.Accepted, updated.Status);
	}

	[Fact]
	public async Task UpdateOrderHandler_ReturnsFalse_WhenMissing()
	{
		await using var context = CreateContext();
		var handler = new UpdateOrderCommandHandler(CreateRepo(context), Mapper, NullLogger<UpdateOrderCommandHandler>.Instance);

		var ok = await handler.Handle(new UpdateOrderCommand { Id = 9999, UserName = "x", EmailAddress = "a@b.com", TotalPrice = 1 }, CancellationToken.None);

		Assert.False(ok);
	}

	[Fact]
	public async Task DeleteOrderHandler_RemovesOrder()
	{
		await using var context = CreateContext();
		var seeded = new Order { UserName = "swn", EmailAddress = "a@b.com", TotalPrice = 10 };
		context.Orders.Add(seeded);
		await context.SaveChangesAsync();
		var id = seeded.Id;
		context.ChangeTracker.Clear();

		var handler = new DeleteOrderCommandHandler(CreateRepo(context), NullLogger<DeleteOrderCommandHandler>.Instance);

		var ok = await handler.Handle(new DeleteOrderCommand { Id = id }, CancellationToken.None);

		Assert.True(ok);
		Assert.Null(await context.Orders.FindAsync(id));
	}

	[Fact]
	public void Order_DomainEvents_AreRaised()
	{
		var order = new Order { UserName = "swn", TotalPrice = 1 };
		order.MarkCreated();
		order.MarkUpdated();
		order.MarkDeleted();

		Assert.Equal(3, order.DomainEvents.Count);

		order.ClearDomainEvents();
		Assert.Empty(order.DomainEvents);
	}

	private static OrderContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<OrderContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new OrderContext(options);
	}

	private static OrderRepository CreateRepo(OrderContext context)
	{
		var uow = new UnitOfWork<OrderContext>(context);
		return new OrderRepository(context, uow);
	}
}
