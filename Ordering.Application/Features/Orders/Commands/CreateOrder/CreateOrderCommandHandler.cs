using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Common.Interfaces;
using Ordering.Domain.Entities;
using Shared.DTOs.Ordering;

namespace Ordering.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
	private readonly IOrderRepository _repository;
	private readonly IMapper _mapper;
	private readonly ILogger<CreateOrderCommandHandler> _logger;

	public CreateOrderCommandHandler(IOrderRepository repository, IMapper mapper, ILogger<CreateOrderCommandHandler> logger)
	{
		_repository = repository;
		_mapper = mapper;
		_logger = logger;
	}

	public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
	{
		var entity = _mapper.Map<Order>(request);
		await _repository.CreateOrderAsync(entity, cancellationToken);
		await _repository.SaveChangesAsync(cancellationToken);
		entity.MarkCreated();

		_logger.LogInformation("Order {OrderId} created for {UserName}", entity.Id, entity.UserName);
		return _mapper.Map<OrderDto>(entity);
	}
}
