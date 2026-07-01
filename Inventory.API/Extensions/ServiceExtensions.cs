using Common.Auth;
using Inventory.API.Persistence;
using Inventory.API.Repositories;
using Inventory.API.Services;

namespace Inventory.API.Extensions;

public static class ServiceExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddControllers();
		services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen();
		services.AddGrpc();

		var mongoSettings = configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>()
			?? throw new InvalidOperationException("MongoDbSettings section is missing.");
		services.AddSingleton(mongoSettings);
		services.AddSingleton<IInventoryContext, InventoryContext>();

		services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()));
		services.AddScoped<IInventoryRepository, InventoryRepository>();
		services.AddScoped<IInventoryService, InventoryService>();

		services.AddHealthChecks()
			.AddMongoDb(mongoSettings.ConnectionString, name: "inventory-mongo", tags: new[] { "ready", "db" });

		services.AddMicroserviceAuthentication(configuration);

		return services;
	}
}
