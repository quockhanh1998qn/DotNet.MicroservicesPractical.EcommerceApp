using AutoMapper;
using Customer.API;
using Customer.API.Entities;
using Customer.API.Persistence;
using Customer.API.Repositories;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Customer;

namespace Customer.API.UnitTests;

public class CustomerApiTests
{
	private static readonly IMapper Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

	[Fact]
	public void MappingProfile_MapsCreateCustomerDto_ToCustomerEntity()
	{
		var source = new CreateCustomerDto
		{
			FirstName = "John",
			LastName = "Doe",
			Email = "john.map@example.com"
		};

		var entity = Mapper.Map<CustomerEntity>(source);

		Assert.Equal(source.FirstName, entity.FirstName);
		Assert.Equal(source.LastName, entity.LastName);
		Assert.Equal(source.Email, entity.Email);
	}

	[Fact]
	public async Task CustomerRepository_CreateCustomer_ThenSave_PersistsEntity()
	{
		await using var context = CreateContext();
		var repository = CreateRepository(context);

		var entity = new CustomerEntity
		{
			FirstName = "Alice",
			LastName = "Walker",
			Email = "alice.walker@example.com"
		};

		await repository.CreateCustomer(entity);
		await repository.SaveChangesAsync();

		var persisted = await repository.GetCustomerByEmail("alice.walker@example.com");
		Assert.NotNull(persisted);
		Assert.Equal("Alice", persisted!.FirstName);
	}

	[Fact]
	public async Task CustomerRepository_GetCustomerByEmail_WhenMissing_ReturnsNull()
	{
		await using var context = CreateContext();
		var repository = CreateRepository(context);

		var customer = await repository.GetCustomerByEmail("missing@example.com");

		Assert.Null(customer);
	}

	[Fact]
	public async Task CustomerRepository_DeleteCustomer_RemovesEntity()
	{
		await using var context = CreateContext();
		var seeded = new CustomerEntity
		{
			FirstName = "Delete",
			LastName = "Me",
			Email = "delete.me@example.com"
		};
		context.Customers.Add(seeded);
		await context.SaveChangesAsync();
		var id = seeded.Id;
		context.ChangeTracker.Clear();

		var repository = CreateRepository(context);
		await repository.DeleteCustomer(id);
		await repository.SaveChangesAsync();

		var deleted = await repository.GetCustomer(id);
		Assert.Null(deleted);
	}

	private static CustomerContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<CustomerContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new CustomerContext(options);
	}

	private static CustomerRepository CreateRepository(CustomerContext context)
	{
		var unitOfWork = new UnitOfWork<CustomerContext>(context);
		return new CustomerRepository(context, unitOfWork);
	}
}
