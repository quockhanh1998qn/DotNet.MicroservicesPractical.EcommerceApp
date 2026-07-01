using AutoMapper;
using EventBus.Messages.IntegrationEvents.Events;
using MassTransit;
using MediatR;
using Ordering.Application.Features.Orders.Commands.CreateOrder;

namespace Ordering.API.EventBusConsumer;

public class BasketCheckoutEventConsumer : IConsumer<BasketCheckoutEvent>
{
	private readonly IMediator _mediator;
	private readonly IMapper _mapper;
	private readonly ILogger<BasketCheckoutEventConsumer> _logger;

	public BasketCheckoutEventConsumer(IMediator mediator, IMapper mapper, ILogger<BasketCheckoutEventConsumer> logger)
	{
		_mediator = mediator;
		_mapper = mapper;
		_logger = logger;
	}

	public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
	{
		var command = _mapper.Map<CreateOrderCommand>(context.Message);
		var result = await _mediator.Send(command, context.CancellationToken);

		_logger.LogInformation(
			"BasketCheckoutEvent consumed: created Order {OrderId} for {UserName}",
			result.Id, result.UserName);
	}
}
