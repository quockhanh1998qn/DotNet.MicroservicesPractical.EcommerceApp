using Microsoft.EntityFrameworkCore;

namespace Customer.API.Extensions;

public static class HostExtensions
{
	public static IHost MigrateDatabase<TContext>(this IHost host, Action<TContext, IServiceProvider> seeder)
		where TContext : DbContext
	{
		using var scope = host.Services.CreateScope();
		var services = scope.ServiceProvider;
		var logger = services.GetRequiredService<ILogger<TContext>>();
		var context = services.GetRequiredService<TContext>();

		try
		{
			logger.LogInformation("Migrating postgres database");
			context.Database.Migrate();
			logger.LogInformation("Migrated postgres database");
			seeder(context, services);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An error occurred while migrating the postgres database");
		}

		return host;
	}
}
