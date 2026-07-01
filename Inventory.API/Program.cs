using Common.Logging;
using Common.Logging.CorrelationId;
using Inventory.API.Extensions;
using Inventory.API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);
Log.Information("Start Inventory API up");

try
{
	builder.Services.AddInfrastructure(builder.Configuration);

	var app = builder.Build();

	app.UseCorrelationId();

	app.UseSwagger();
	app.UseSwaggerUI();
	app.UseAuthentication();
	app.UseAuthorization();
	app.MapControllers();
	app.MapGrpcService<StockProtoServiceImpl>();
	app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
	{
		Predicate = _ => true,
		ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse,
	});

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Unhandled exception");
}
finally
{
	Log.Information("Shut down Inventory API complete");
	Log.CloseAndFlush();
}

