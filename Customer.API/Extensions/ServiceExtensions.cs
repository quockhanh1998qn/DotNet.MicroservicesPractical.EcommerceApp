using Common.Auth;
using Contracts.Common.Interfaces;
using Customer.API.Persistence;
using Customer.API.Repositories;
using Customer.API.Repositories.Interfaces;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Customer.API.Extensions;

public static class ServiceExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
		services.AddAuthorization();
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen();
		services.ConfigureCustomerDbContext(configuration);
		services.AddInfrastructureServices();
		services.AddMicroserviceAuthentication(configuration);

		var connectionString = configuration.GetConnectionString("DefaultConnectionString") ?? string.Empty;
		services.AddHealthChecks()
			.AddNpgSql(connectionString, name: "customerdb-postgres", tags: new[] { "ready", "db" });

		return services;
	}

	private static IServiceCollection ConfigureCustomerDbContext(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString(name: "DefaultConnectionString")
			?? throw new InvalidOperationException("Connection string 'DefaultConnectionString' is missing.");
		var builder = new NpgsqlConnectionStringBuilder(connectionString);

		services.AddDbContext<CustomerContext>(m =>
			m.UseNpgsql(builder.ConnectionString, e =>
			{
				e.MigrationsAssembly("Customer.API");
			}));

		services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()));

		return services;
	}

	private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
	{
		return services.AddScoped(typeof(IRepositoryBaseAsync<,,>), typeof(RepositoryBaseAsync<,,>))
						.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>))
						.AddScoped(typeof(ICustomerRepository), typeof(CustomerRepository));
	}
}
