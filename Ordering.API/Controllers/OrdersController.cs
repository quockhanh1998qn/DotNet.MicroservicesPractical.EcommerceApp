using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.Features.Orders.Commands.CreateOrder;
using Ordering.Application.Features.Orders.Commands.DeleteOrder;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrderById;
using Ordering.Application.Features.Orders.Queries.GetOrdersByUserName;
using Shared.DTOs.Ordering;

namespace Ordering.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
	private readonly IMediator _mediator;

	public OrdersController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet("{userName}", Name = nameof(GetOrdersByUserName))]
	[ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersByUserName([FromRoute] string userName, CancellationToken cancellationToken)
	{
		var result = await _mediator.Send(new GetOrdersByUserNameQuery(userName), cancellationToken);
		return Ok(result);
	}

	[HttpGet("id/{id:long}", Name = nameof(GetOrderById))]
	[ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<OrderDto>> GetOrderById([FromRoute] long id, CancellationToken cancellationToken)
	{
		var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
		return result is null ? NotFound() : Ok(result);
	}

	[HttpPost]
	[ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
	public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
	{
		var result = await _mediator.Send(command, cancellationToken);
		return CreatedAtRoute(nameof(GetOrderById), new { id = result.Id }, result);
	}

	[HttpPut("{id:long}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> UpdateOrder([FromRoute] long id, [FromBody] UpdateOrderCommand command, CancellationToken cancellationToken)
	{
		command.Id = id;
		var ok = await _mediator.Send(command, cancellationToken);
		return ok ? NoContent() : NotFound();
	}

	[HttpDelete("{id:long}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeleteOrder([FromRoute] long id, CancellationToken cancellationToken)
	{
		var ok = await _mediator.Send(new DeleteOrderCommand { Id = id }, cancellationToken);
		return ok ? NoContent() : NotFound();
	}
}
