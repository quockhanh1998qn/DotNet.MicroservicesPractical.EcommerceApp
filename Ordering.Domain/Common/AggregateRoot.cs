using Contracts.Domains;

namespace Ordering.Domain.Common;

public abstract class AggregateRoot<TKey> : EntityAuditBase<TKey>
{
	private readonly List<BaseDomainEvent> _domainEvents = new();

	public IReadOnlyCollection<BaseDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

	protected void AddDomainEvent(BaseDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

	public void ClearDomainEvents() => _domainEvents.Clear();
}
