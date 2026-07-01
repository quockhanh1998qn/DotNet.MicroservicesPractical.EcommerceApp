using Common.Auth;
using Common.Logging;
using Common.Logging.CorrelationId;
using EventBus.Messages.IntegrationEvents.Common;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.API.EventBusConsumer;
using Ordering.Application;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);
Log.Information("Start Ordering API up");

try
{
	builder.Services.AddControllers();
	builder.Services.AddFluentValidationAutoValidation();
	builder.Services.AddFluentValidationClientsideAdapters();
	builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();

	builder.Services.AddApplicationServices();
	builder.Services.AddInfrastructureServices(builder.Configuration);
	builder.Services.AddAutoMapper(cfg => cfg.AddProfile(new OrderingEventBusMappingProfile()));

	var orderingConnection = builder.Configuration.GetConnectionString("DefaultConnectionString") ?? string.Empty;
	var rabbitmqHost = builder.Configuration["EventBusSettings:HostAddress"] ?? "amqp://guest:guest@localhost:5672";
	builder.Services.AddHealthChecks()
		.AddSqlServer(orderingConnection, name: "orderdb-sqlserver", tags: new[] { "ready", "db" })
		.AddRabbitMQ(new Uri(rabbitmqHost), name: "rabbitmq", tags: new[] { "ready", "bus" });

	builder.Services.AddMicroserviceAuthentication(builder.Configuration);

	var eventBusHost = builder.Configuration.GetSection("EventBusSettings:HostAddress").Value
		?? "amqp://guest:guest@localhost:5672";

	builder.Services.AddMassTransit(config =>
	{
		config.AddConsumer<BasketCheckoutEventConsumer>();
		config.UsingRabbitMq((ctx, cfg) =>
		{
			cfg.Host(new Uri(eventBusHost));
			cfg.ReceiveEndpoint(EventBusConstants.BasketCheckoutQueue, e =>
			{
				// Transactional Outbox pattern: buffer outbound publishes/sends until
				// the consumer completes successfully. Retries also benefit from this.
				e.UseMessageRetry(r => r.Interval(retryCount: 3, interval: TimeSpan.FromSeconds(2)));
				e.UseInMemoryOutbox(ctx);
				e.ConfigureConsumer<BasketCheckoutEventConsumer>(ctx);
			});
		});
	});

	var app = builder.Build();

	app.UseCorrelationId();

	app.UseSwagger();
	app.UseSwaggerUI();
	app.UseAuthentication();
	app.UseAuthorization();
	app.MapControllers();
	app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
	{
		Predicate = _ => true,
		ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse,
	});

	using (var scope = app.Services.CreateScope())
	{
		var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
		var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrderContext>>();
		try
		{
			await context.Database.EnsureCreatedAsync();
			await Ordering.Infrastructure.Persistence.OrderContextSeed.SeedOrderAsync(context, logger);
		}
		catch (Exception migrationEx)
		{
			logger.LogError(migrationEx, "Failed to initialize OrderContext");
		}
	}

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Unhandled exception");
}
finally
{
	Log.Information("Shut down Ordering API complete");
	Log.CloseAndFlush();
}

