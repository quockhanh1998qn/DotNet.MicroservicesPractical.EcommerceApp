using Identity.Server.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Server.Persistence;

public class IdentityContext : IdentityDbContext<User, Role, long>
{
	public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
	{
	}

	public DbSet<Permission> Permissions => Set<Permission>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<User>(b =>
		{
			b.ToTable("AppUsers");
			b.Property(x => x.FirstName).HasMaxLength(100);
			b.Property(x => x.LastName).HasMaxLength(100);
		});

		builder.Entity<Role>(b =>
		{
			b.ToTable("AppRoles");
			b.Property(x => x.Description).HasMaxLength(200);
		});

		builder.Entity<Permission>(b =>
		{
			b.ToTable("AppPermissions");
			b.HasKey(x => x.Id);
			b.Property(x => x.Id).ValueGeneratedOnAdd();
			b.Property(x => x.Function).HasMaxLength(100).IsRequired();
			b.Property(x => x.Command).HasMaxLength(50).IsRequired();
			b.HasIndex(x => new { x.RoleId, x.Function, x.Command }).IsUnique();
		});
	}
}
