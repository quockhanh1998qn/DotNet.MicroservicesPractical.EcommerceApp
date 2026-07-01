using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Entities;

namespace Ordering.Infrastructure.Persistence;

public class OrderContext : DbContext
{
	public OrderContext(DbContextOptions<OrderContext> options) : base(options)
	{
	}

	public DbSet<Order> Orders => Set<Order>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Order>(b =>
		{
			b.ToTable("Orders");
			b.HasKey(x => x.Id);
			b.Property(x => x.Id).ValueGeneratedOnAdd();
			b.Property(x => x.UserName).HasMaxLength(100).IsRequired();
			b.Property(x => x.FirstName).HasMaxLength(100);
			b.Property(x => x.LastName).HasMaxLength(100);
			b.Property(x => x.EmailAddress).HasMaxLength(320);
			b.Property(x => x.ShippingAddress).HasMaxLength(500);
			b.Property(x => x.InvoiceAddress).HasMaxLength(500);
			b.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
			b.Property(x => x.Status).HasConversion<int>();
			b.Property(x => x.CreatedDate);
			b.Property(x => x.LastModifiedDate);
			b.Ignore(x => x.DomainEvents);
		});
	}

	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		foreach (var entry in ChangeTracker.Entries<Order>())
		{
			switch (entry.State)
			{
				case EntityState.Added:
					entry.Entity.CreatedDate = DateTimeOffset.UtcNow;
					break;
				case EntityState.Modified:
					entry.Entity.LastModifiedDate = DateTimeOffset.UtcNow;
					break;
			}
		}

		return base.SaveChangesAsync(cancellationToken);
	}
}
