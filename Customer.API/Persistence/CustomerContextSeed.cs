using Customer.API.Entities;
using Serilog;

namespace Customer.API.Persistence;

public static class CustomerContextSeed
{
	public static async Task SeedCustomerAsync(CustomerContext customerContext, Serilog.ILogger logger)
	{
		if (!customerContext.Customers.Any())
		{
			customerContext.Customers.AddRange(GetSeedCustomers());
			await customerContext.SaveChangesAsync();

			logger.Information("Seeded data for Customer DB associated with context {DbContextName}", nameof(CustomerContext));
		}
	}

	private static IEnumerable<CustomerEntity> GetSeedCustomers()
	{
		return new List<CustomerEntity>
		{
			new() { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
			new() { FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com" },
			new() { FirstName = "Alex", LastName = "Johnson", Email = "alex.johnson@example.com" }
		};
	}
}
