using MediatR;
using Shared.DTOs.Ordering;

namespace Ordering.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<OrderDto>
{
	public string UserName { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string EmailAddress { get; set; } = string.Empty;
	public string ShippingAddress { get; set; } = string.Empty;
	public string InvoiceAddress { get; set; } = string.Empty;
	public decimal TotalPrice { get; set; }
}
