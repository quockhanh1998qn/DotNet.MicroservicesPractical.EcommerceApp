using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Common.Interfaces;

namespace Ordering.Application.Features.Orders.Commands.DeleteOrder;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, bool>
{
	private readonly IOrderRepository _repository;
	private readonly ILogger<DeleteOrderCommandHandler> _logger;

	public DeleteOrderCommandHandler(IOrderRepository repository, ILogger<DeleteOrderCommandHandler> logger)
	{
		_repository = repository;
		_logger = logger;
	}

	public async Task<bool> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
	{
		var entity = await _repository.GetOrderByIdAsync(request.Id, cancellationToken);
		if (entity is null)
		{
			return false;
		}

		entity.MarkDeleted();
		await _repository.DeleteOrderAsync(entity, cancellationToken);
		await _repository.SaveChangesAsync(cancellationToken);
		_logger.LogInformation("Order {OrderId} deleted", entity.Id);
		return true;
	}
}
