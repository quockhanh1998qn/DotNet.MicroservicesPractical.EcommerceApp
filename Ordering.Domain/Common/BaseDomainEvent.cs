namespace Ordering.Domain.Common;

public abstract class BaseDomainEvent
{
	public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
	public Guid EventId { get; } = Guid.NewGuid();
}
