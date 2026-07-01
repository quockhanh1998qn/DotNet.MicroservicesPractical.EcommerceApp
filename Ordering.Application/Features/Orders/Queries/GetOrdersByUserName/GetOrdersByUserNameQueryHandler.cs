using AutoMapper;
using MediatR;
using Ordering.Application.Common.Interfaces;
using Shared.DTOs.Ordering;

namespace Ordering.Application.Features.Orders.Queries.GetOrdersByUserName;

public class GetOrdersByUserNameQueryHandler : IRequestHandler<GetOrdersByUserNameQuery, IReadOnlyList<OrderDto>>
{
	private readonly IOrderRepository _repository;
	private readonly IMapper _mapper;

	public GetOrdersByUserNameQueryHandler(IOrderRepository repository, IMapper mapper)
	{
		_repository = repository;
		_mapper = mapper;
	}

	public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersByUserNameQuery request, CancellationToken cancellationToken)
	{
		var orders = await _repository.GetOrdersByUserNameAsync(request.UserName, cancellationToken);
		return _mapper.Map<IReadOnlyList<OrderDto>>(orders);
	}
}
