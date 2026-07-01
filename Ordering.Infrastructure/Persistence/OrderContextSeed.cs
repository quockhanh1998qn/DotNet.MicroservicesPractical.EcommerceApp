using Microsoft.Extensions.Logging;
using Ordering.Domain.Entities;
using Ordering.Domain.ValueObjects;

namespace Ordering.Infrastructure.Persistence;

public static class OrderContextSeed
{
	public static async Task SeedOrderAsync(OrderContext context, ILogger logger)
	{
		if (!context.Orders.Any())
		{
			context.Orders.AddRange(GetOrders());
			await context.SaveChangesAsync();
			logger.LogInformation("Seeded data for OrderContext");
		}
	}

	private static IEnumerable<Order> GetOrders() => new List<Order>
	{
		new()
		{
			UserName = "swn",
			FirstName = "Tedu",
			LastName = "User",
			EmailAddress = "swn@example.com",
			ShippingAddress = "123 Main St",
			InvoiceAddress = "123 Main St",
			TotalPrice = 350m,
			Status = OrderStatus.NotStarted
		}
	};
}
