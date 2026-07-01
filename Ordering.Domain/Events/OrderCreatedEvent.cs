using Ordering.Domain.Common;

namespace Ordering.Domain.Events;

public class OrderCreatedEvent : BaseDomainEvent
{
	public long OrderId { get; }
	public string UserName { get; }
	public decimal TotalPrice { get; }

	public OrderCreatedEvent(long orderId, string userName, decimal totalPrice)
	{
		OrderId = orderId;
		UserName = userName;
		TotalPrice = totalPrice;
	}
}

public class OrderUpdatedEvent : BaseDomainEvent
{
	public long OrderId { get; }
	public string UserName { get; }
	public decimal TotalPrice { get; }

	public OrderUpdatedEvent(long orderId, string userName, decimal totalPrice)
	{
		OrderId = orderId;
		UserName = userName;
		TotalPrice = totalPrice;
	}
}

public class OrderDeletedEvent : BaseDomainEvent
{
	public long OrderId { get; }
	public string UserName { get; }

	public OrderDeletedEvent(long orderId, string userName)
	{
		OrderId = orderId;
		UserName = userName;
	}
}
