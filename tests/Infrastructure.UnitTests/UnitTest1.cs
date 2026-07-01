using Contracts.Domains;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UnitTests;

public class RepositoryAndUnitOfWorkTests
{
	[Fact]
	public async Task UnitOfWork_SaveChangesAsync_PersistsEntities()
	{
		await using var context = CreateContext();
		var unitOfWork = new UnitOfWork<TestDbContext>(context);

		context.Entities.Add(new TestEntity { Id = 1, Name = "entity-1" });

		var savedCount = await unitOfWork.SaveChangesAsync();

		Assert.Equal(1, savedCount);
		Assert.Single(context.Entities);
	}

	[Fact]
	public async Task Repository_CreateAsync_ThenSaveChanges_PersistsEntity()
	{
		await using var context = CreateContext();
		var unitOfWork = new UnitOfWork<TestDbContext>(context);
		var repository = new RepositoryBaseAsync<TestEntity, int, TestDbContext>(context, unitOfWork);

		await repository.CreateAsync(new TestEntity { Id = 7, Name = "new-entity" });
		await repository.SaveChangesAsync();

		var entity = await context.Entities.SingleOrDefaultAsync(x => x.Id == 7);
		Assert.NotNull(entity);
		Assert.Equal("new-entity", entity!.Name);
	}

	[Fact]
	public async Task Repository_UpdateAsync_ThenSaveChanges_UpdatesEntity()
	{
		await using var context = CreateContext();
		context.Entities.Add(new TestEntity { Id = 10, Name = "before-update" });
		await context.SaveChangesAsync();
		context.ChangeTracker.Clear();

		var unitOfWork = new UnitOfWork<TestDbContext>(context);
		var repository = new RepositoryBaseAsync<TestEntity, int, TestDbContext>(context, unitOfWork);

		await repository.UpdateAsync(new TestEntity { Id = 10, Name = "after-update" });
		await repository.SaveChangesAsync();

		var entity = await context.Entities.SingleAsync(x => x.Id == 10);
		Assert.Equal("after-update", entity.Name);
	}

	[Fact]
	public async Task Repository_DeleteAsync_ThenSaveChanges_RemovesEntity()
	{
		await using var context = CreateContext();
		context.Entities.Add(new TestEntity { Id = 99, Name = "to-delete" });
		await context.SaveChangesAsync();

		var unitOfWork = new UnitOfWork<TestDbContext>(context);
		var repository = new RepositoryBaseAsync<TestEntity, int, TestDbContext>(context, unitOfWork);

		var entity = await context.Entities.SingleAsync(x => x.Id == 99);
		await repository.DeleteAsync(entity);
		await repository.SaveChangesAsync();

		Assert.False(await context.Entities.AnyAsync(x => x.Id == 99));
	}

	private static TestDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new TestDbContext(options);
	}

	private sealed class TestDbContext : DbContext
	{
		public TestDbContext(DbContextOptions<TestDbContext> options)
			: base(options)
		{
		}

		public DbSet<TestEntity> Entities => Set<TestEntity>();
	}

	private sealed class TestEntity : EntityBase<int>
	{
		public string Name { get; set; } = string.Empty;
	}
}
