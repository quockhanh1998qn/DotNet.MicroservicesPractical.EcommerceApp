using Contracts.Domains.Interfaces;

namespace Contracts.Domains;

public abstract class EntityAuditBase<T> : EntityBase<T>, IAuditTable
{
	public DateTimeOffset CreatedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set;}
}
