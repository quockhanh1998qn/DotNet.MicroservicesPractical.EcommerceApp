using Ordering.Domain.Common;
using Ordering.Domain.Events;
using Ordering.Domain.ValueObjects;

namespace Ordering.Domain.Entities;

public class Order : AggregateRoot<long>
{
	public string UserName { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string EmailAddress { get; set; } = string.Empty;
	public string ShippingAddress { get; set; } = string.Empty;
	public string InvoiceAddress { get; set; } = string.Empty;
	public decimal TotalPrice { get; set; }
	public OrderStatus Status { get; set; } = OrderStatus.NotStarted;

	public void MarkCreated() => AddDomainEvent(new OrderCreatedEvent(Id, UserName, TotalPrice));
	public void MarkUpdated() => AddDomainEvent(new OrderUpdatedEvent(Id, UserName, TotalPrice));
	public void MarkDeleted() => AddDomainEvent(new OrderDeletedEvent(Id, UserName));
}
