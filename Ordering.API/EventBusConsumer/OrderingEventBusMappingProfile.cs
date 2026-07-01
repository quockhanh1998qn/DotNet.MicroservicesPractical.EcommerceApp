using AutoMapper;
using EventBus.Messages.IntegrationEvents.Events;
using Ordering.Application.Features.Orders.Commands.CreateOrder;

namespace Ordering.API.EventBusConsumer;

public class OrderingEventBusMappingProfile : Profile
{
	public OrderingEventBusMappingProfile()
	{
		CreateMap<BasketCheckoutEvent, CreateOrderCommand>();
	}
}
