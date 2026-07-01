using Common.Logging;
using Common.Logging.CorrelationId;
using HangFire.API.Extensions;
using Hangfire;
using Microsoft.Extensions.Options;
using Serilog;
using Shared.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);
Log.Information("Start HangFire API up");

try
{
	builder.Services.AddInfrastructure(builder.Configuration);

	var app = builder.Build();

	app.UseCorrelationId();

	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	app.UseAuthentication();
	app.UseAuthorization();

	var hangfireSettings = app.Services.GetRequiredService<IOptions<HangfireSettings>>().Value;
	app.UseHangfireDashboard(hangfireSettings.Route, new DashboardOptions
	{
		DashboardTitle = hangfireSettings.DashboardTitle,
		Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
		IgnoreAntiforgeryToken = true,
	});

	app.MapControllers();
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
	Log.Information("Shut down HangFire API complete");
	Log.CloseAndFlush();
}

