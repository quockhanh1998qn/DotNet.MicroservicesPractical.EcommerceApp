using MediatR;
using Shared.DTOs.Ordering;

namespace Ordering.Application.Features.Orders.Queries.GetOrdersByUserName;

public class GetOrdersByUserNameQuery : IRequest<IReadOnlyList<OrderDto>>
{
	public string UserName { get; }

	public GetOrdersByUserNameQuery(string userName)
	{
		UserName = userName;
	}
}
