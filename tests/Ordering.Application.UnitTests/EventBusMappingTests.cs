using AutoMapper;
using EventBus.Messages.IntegrationEvents.Events;
using Ordering.API.EventBusConsumer;
using Ordering.Application.Features.Orders.Commands.CreateOrder;

namespace Ordering.Application.UnitTests;

public class EventBusMappingTests
{
	[Fact]
	public void BasketCheckoutEvent_MapsTo_CreateOrderCommand()
	{
		var mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderingEventBusMappingProfile>()).CreateMapper();

		var evt = new BasketCheckoutEvent
		{
			UserName = "swn",
			TotalPrice = 99.5m,
			FirstName = "T",
			LastName = "U",
			EmailAddress = "a@b.com",
			ShippingAddress = "ship",
			InvoiceAddress = "inv"
		};

		var cmd = mapper.Map<CreateOrderCommand>(evt);

		Assert.Equal("swn", cmd.UserName);
		Assert.Equal(99.5m, cmd.TotalPrice);
		Assert.Equal("a@b.com", cmd.EmailAddress);
		Assert.Equal("ship", cmd.ShippingAddress);
	}
}
