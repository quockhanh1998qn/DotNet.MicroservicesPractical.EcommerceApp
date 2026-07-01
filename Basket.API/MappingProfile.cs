using AutoMapper;
using Basket.API.Entities;
using EventBus.Messages.IntegrationEvents.Events;
using Shared.DTOs.Basket;

namespace Basket.API;

public class MappingProfile : Profile
{
	public MappingProfile()
	{
		CreateMap<Cart, CartDto>().ReverseMap();
		CreateMap<CartItem, CartItemDto>().ReverseMap();
		CreateMap<BasketCheckoutDto, BasketCheckoutEvent>();
	}
}
