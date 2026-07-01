using AutoMapper;
using Ordering.Application.Features.Orders.Commands.CreateOrder;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Domain.Entities;
using Ordering.Domain.ValueObjects;
using Shared.DTOs.Ordering;

namespace Ordering.Application.Mappings;

public class OrderingMappingProfile : Profile
{
	public OrderingMappingProfile()
	{
		CreateMap<Order, OrderDto>()
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status));

		CreateMap<CreateOrderCommand, Order>()
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => OrderStatus.NotStarted));

		CreateMap<UpdateOrderCommand, Order>()
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => (OrderStatus)src.Status));
	}
}
