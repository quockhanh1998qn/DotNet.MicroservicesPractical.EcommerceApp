using Common.Logging.CorrelationId;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Product.API.Extensions;

public static class ApplicationExtensions
{
	public static void UseInfrastructure(this IApplicationBuilder app)
	{
		app.UseCorrelationId();
		// Configure the HTTP request pipeline.
		app.UseSwagger();
		app.UseSwaggerUI();

		app.UseRouting();
		//app.UseHttpsRedirection(); -- for PROD only

		app.UseAuthentication();
		app.UseAuthorization();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapDefaultControllerRoute();
			endpoints.MapHealthChecks("/health", new HealthCheckOptions
			{
				Predicate = _ => true,
				ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
			});
		});
	}
}
