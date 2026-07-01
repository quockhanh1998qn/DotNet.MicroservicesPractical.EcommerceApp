using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Common.Interfaces;

namespace Ordering.Application.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, bool>
{
	private readonly IOrderRepository _repository;
	private readonly IMapper _mapper;
	private readonly ILogger<UpdateOrderCommandHandler> _logger;

	public UpdateOrderCommandHandler(IOrderRepository repository, IMapper mapper, ILogger<UpdateOrderCommandHandler> logger)
	{
		_repository = repository;
		_mapper = mapper;
		_logger = logger;
	}

	public async Task<bool> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
	{
		var entity = await _repository.GetOrderByIdAsync(request.Id, cancellationToken);
		if (entity is null)
		{
			return false;
		}

		_mapper.Map(request, entity);
		await _repository.UpdateOrderAsync(entity, cancellationToken);
		await _repository.SaveChangesAsync(cancellationToken);
		entity.MarkUpdated();

		_logger.LogInformation("Order {OrderId} updated", entity.Id);
		return true;
	}
}
