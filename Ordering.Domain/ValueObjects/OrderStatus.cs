namespace Ordering.Domain.ValueObjects;

public enum OrderStatus
{
	NotStarted = 0,
	Accepted = 1,
	Cancelled = 2,
	Completed = 3,
	Returned = 4
}
