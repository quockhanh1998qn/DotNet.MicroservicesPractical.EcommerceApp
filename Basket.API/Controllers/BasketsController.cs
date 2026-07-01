using AutoMapper;
using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using Basket.API.Services;
using EventBus.Messages.IntegrationEvents.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Basket;

namespace Basket.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasketsController : ControllerBase
{
	private readonly IBasketRepository _basketRepository;
	private readonly IMapper _mapper;
	private readonly IPublishEndpoint _publishEndpoint;
	private readonly IStockService _stockService;

	public BasketsController(
		IBasketRepository basketRepository,
		IMapper mapper,
		IPublishEndpoint publishEndpoint,
		IStockService stockService)
	{
		_basketRepository = basketRepository;
		_mapper = mapper;
		_publishEndpoint = publishEndpoint;
		_stockService = stockService;
	}

	[HttpGet("{username}", Name = nameof(GetBasket))]
	[ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<CartDto>> GetBasket([FromRoute] string username, CancellationToken cancellationToken)
	{
		var cart = await _basketRepository.GetBasketAsync(username, cancellationToken);
		if (cart is null)
		{
			return NotFound();
		}

		return Ok(_mapper.Map<CartDto>(cart));
	}

	[HttpPost]
	[ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<CartDto>> UpdateBasket([FromBody] CartDto cartDto, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}

		var cart = _mapper.Map<Cart>(cartDto);

		foreach (var item in cart.Items)
		{
			var available = await _stockService.GetStockAsync(item.ProductNo, cancellationToken);
			if (available < item.Quantity)
			{
				ModelState.AddModelError(nameof(item.Quantity), $"Insufficient stock for '{item.ProductNo}'. Available: {available}, Requested: {item.Quantity}.");
				return ValidationProblem(ModelState);
			}
		}

		var saved = await _basketRepository.UpdateBasketAsync(cart, options: null, cancellationToken);
		return Ok(_mapper.Map<CartDto>(saved));
	}

	[HttpDelete("{username}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> DeleteBasket([FromRoute] string username, CancellationToken cancellationToken)
	{
		await _basketRepository.DeleteBasketAsync(username, cancellationToken);
		return NoContent();
	}

	[HttpPost("checkout")]
	[ProducesResponseType(StatusCodes.Status202Accepted)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Checkout([FromBody] BasketCheckoutDto basketCheckoutDto, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid)
		{
			return ValidationProblem(ModelState);
		}

		var basket = await _basketRepository.GetBasketAsync(basketCheckoutDto.UserName, cancellationToken);
		if (basket is null)
		{
			return NotFound(new { message = "Basket not found for user." });
		}

		var eventMessage = _mapper.Map<BasketCheckoutEvent>(basketCheckoutDto);
		eventMessage.TotalPrice = basket.TotalPrice;

		await _publishEndpoint.Publish(eventMessage, cancellationToken);
		await _basketRepository.DeleteBasketAsync(basketCheckoutDto.UserName, cancellationToken);

		return Accepted();
	}
}
