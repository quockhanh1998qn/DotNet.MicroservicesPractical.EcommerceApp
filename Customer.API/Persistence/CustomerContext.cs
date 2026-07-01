using Contracts.Domains.Interfaces;
using Customer.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Persistence;

public class CustomerContext : DbContext
{
	public CustomerContext(DbContextOptions<CustomerContext> options)
		: base(options)
	{
	}

	public DbSet<CustomerEntity> Customers { get; set; } = null!;

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var modified = ChangeTracker.Entries()
			.Where(e => e.State == EntityState.Modified ||
						e.State == EntityState.Added ||
						e.State == EntityState.Deleted);

		foreach (var item in modified)
		{
			switch (item.State)
			{
				case EntityState.Added:
					if (item.Entity is IDateTracking addedEntity)
					{
						addedEntity.CreatedDate = DateTime.UtcNow;
					}

					break;

				case EntityState.Modified:
					Entry(item.Entity).Property("Id").IsModified = false;
					if (item.Entity is IDateTracking modifiedEntity)
					{
						modifiedEntity.LastModifiedDate = DateTime.UtcNow;
					}

					break;
			}
		}

		return base.SaveChangesAsync(cancellationToken);
	}
}
