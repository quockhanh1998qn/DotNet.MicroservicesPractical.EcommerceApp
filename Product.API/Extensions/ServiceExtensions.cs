using Common.Auth;
using Contracts.Common.Interfaces;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Product.API.Persistence;
using Product.API.Repositories;
using Product.API.Repositories.Interfaces;

namespace Product.API.Extensions;

public static class ServiceExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddControllers();
		services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

		// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen();
		services.ConfigureProductDbContext(configuration);
		services.AddInfrastructureServices();
		services.AddHealthCheckServices(configuration);
		services.AddMicroserviceAuthentication(configuration);

		return services;
	}

	private static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString("DefaultConnectionString") ?? string.Empty;
		services.AddHealthChecks()
			.AddMySql(connectionString, name: "productdb-mysql", tags: new[] { "ready", "db" });
		return services;
	}

	private static IServiceCollection ConfigureProductDbContext(this IServiceCollection services, IConfiguration configuration)
	{
		var connectionString = configuration.GetConnectionString(name:"DefaultConnectionString")
			?? throw new InvalidOperationException("Connection string 'DefaultConnectionString' is missing.");
		var builder = new MySqlConnectionStringBuilder(connectionString);

		services.AddDbContext<ProductContext>(m =>
			m.UseMySql(builder.ConnectionString, new MySqlServerVersion(new Version(8, 0, 29)), e =>
			{
				e.MigrationsAssembly("Product.API");
				e.SchemaBehavior(MySqlSchemaBehavior.Ignore);
			}));

		services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()));

		return services;
	}

	private static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
	{
		return services.AddScoped(typeof(IRepositoryBaseAsync<,,>), typeof(RepositoryBaseAsync<,,>))
						.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>))
						.AddScoped(typeof(IProductRepository), typeof(ProductRepository));
	}
}
