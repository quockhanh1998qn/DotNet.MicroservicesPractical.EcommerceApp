using System.Net.Http;
using Common.Auth;
using Basket.API.Repositories;
using Basket.API.Repositories.Interfaces;
using Basket.API.Services;
using Inventory.Grpc.Protos;
using MassTransit;
using Polly;
using Polly.Extensions.Http;

namespace Basket.API.Extensions;

public static class ServiceExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddControllers();
		services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen();

		services.ConfigureRedis(configuration);
		services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()));
		services.ConfigureMassTransit(configuration);
		services.ConfigureGrpc(configuration);
		services.AddHealthCheckServices(configuration);
		services.AddMicroserviceAuthentication(configuration);

		services.AddScoped<IBasketRepository, BasketRepository>();
		services.AddScoped<IStockService, StockService>();

		return services;
	}

	private static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
	{
		var redis = configuration["CacheSettings:ConnectionString"] ?? string.Empty;
		var rabbitmq = configuration["EventBusSettings:HostAddress"] ?? string.Empty;
		var stockGrpc = configuration["GrpcSettings:StockUrl"];

		var builder = services.AddHealthChecks()
			.AddRedis(redis, name: "basket-redis", tags: new[] { "ready", "cache" })
			.AddRabbitMQ(new Uri(string.IsNullOrWhiteSpace(rabbitmq) ? "amqp://guest:guest@localhost:5672" : rabbitmq), name: "rabbitmq", tags: new[] { "ready", "bus" });

		if (!string.IsNullOrWhiteSpace(stockGrpc) && Uri.TryCreate(stockGrpc, UriKind.Absolute, out var grpcUri))
		{
			// gRPC URL points at the HTTP/2 port (e.g. :8081). A plain HTTP GET on the gRPC port
			// returns 400, so probe the sibling HTTP /health endpoint on the standard service port.
			var httpHealthPort = int.TryParse(configuration["GrpcSettings:HealthPort"], out var p) ? p : 8080;
			var healthUri = new UriBuilder("http", grpcUri.Host, httpHealthPort, "/health").Uri;
			builder.AddUrlGroup(healthUri, name: "inventory-grpc", tags: new[] { "ready", "downstream" });
		}

		return services;
	}

	private static IServiceCollection ConfigureGrpc(this IServiceCollection services, IConfiguration configuration)
	{
		var grpcSettings = configuration.GetSection("GrpcSettings:StockUrl").Value
			?? throw new InvalidOperationException("GrpcSettings:StockUrl is missing.");

		services.AddGrpcClient<StockProtoService.StockProtoServiceClient>(o =>
		{
			o.Address = new Uri(grpcSettings);
		})
		.AddPolicyHandler(GetRetryPolicy())
		.AddPolicyHandler(GetCircuitBreakerPolicy())
		.AddPolicyHandler(GetTimeoutPolicy());

		return services;
	}

	private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
		HttpPolicyExtensions
			.HandleTransientHttpError()
			.WaitAndRetryAsync(retryCount: 3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

	private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
		HttpPolicyExtensions
			.HandleTransientHttpError()
			.CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));

	private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
		Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

	private static IServiceCollection ConfigureMassTransit(this IServiceCollection services, IConfiguration configuration)
	{
		var host = configuration.GetSection("EventBusSettings:HostAddress").Value
			?? throw new InvalidOperationException("EventBusSettings:HostAddress is missing.");

		services.AddMassTransit(config =>
		{
			config.UsingRabbitMq((ctx, cfg) =>
			{
				cfg.Host(new Uri(host));
			});
		});

		return services;
	}

	private static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
	{
		var redisConnection = configuration.GetSection("CacheSettings:ConnectionString").Value
			?? throw new InvalidOperationException("CacheSettings:ConnectionString is missing.");

		services.AddStackExchangeRedisCache(options =>
		{
			options.Configuration = redisConnection;
			options.InstanceName = "Basket:";
		});

		return services;
	}
}
