using AutoMapper;
using MediatR;
using Ordering.Application.Common.Interfaces;
using Shared.DTOs.Ordering;

namespace Ordering.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
	private readonly IOrderRepository _repository;
	private readonly IMapper _mapper;

	public GetOrderByIdQueryHandler(IOrderRepository repository, IMapper mapper)
	{
		_repository = repository;
		_mapper = mapper;
	}

	public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
	{
		var order = await _repository.GetOrderByIdAsync(request.Id, cancellationToken);
		return order is null ? null : _mapper.Map<OrderDto>(order);
	}
}
