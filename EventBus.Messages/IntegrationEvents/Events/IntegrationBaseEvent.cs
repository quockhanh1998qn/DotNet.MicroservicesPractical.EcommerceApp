namespace EventBus.Messages.IntegrationEvents.Events;

public abstract class IntegrationBaseEvent
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public DateTime CreationDate { get; set; } = DateTime.UtcNow;
}
