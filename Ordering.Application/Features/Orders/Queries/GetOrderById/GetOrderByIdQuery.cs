using MediatR;
using Shared.DTOs.Ordering;

namespace Ordering.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQuery : IRequest<OrderDto?>
{
	public long Id { get; }

	public GetOrderByIdQuery(long id)
	{
		Id = id;
	}
}
