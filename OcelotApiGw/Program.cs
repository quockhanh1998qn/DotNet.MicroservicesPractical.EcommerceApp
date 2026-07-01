using Common.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);
Log.Information("Start Ocelot Api Gateway up");

try
{
	builder.Configuration.AddOcelot(builder.Environment);

	// Single Identity Server-backed bearer scheme. Each Ocelot route can opt-in via
	// "AuthenticationOptions": { "AuthenticationProviderKey": "Bearer" }.
	var identityAuthority = builder.Configuration["IdentityServer:Authority"] ?? "http://identity.server:8080";
	builder.Services
		.AddAuthentication()
		.AddJwtBearer("Bearer", options =>
		{
			options.Authority = identityAuthority;
			options.RequireHttpsMetadata = false;
			options.Audience = builder.Configuration["IdentityServer:Audience"] ?? "tedu.microservices";
			options.TokenValidationParameters.ValidateAudience =
				!string.IsNullOrWhiteSpace(options.Audience);
		});

	builder.Services
		.AddOcelot(builder.Configuration)
		.AddCacheManager(x => x.WithDictionaryHandle())
		.AddPolly();

	builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
		.WithOrigins("http://localhost:4200", "http://localhost:5100")
		.AllowAnyHeader()
		.AllowAnyMethod()
		.AllowCredentials()));

	var app = builder.Build();

	app.UseCors();
	app.UseAuthentication();
	await app.UseOcelot();

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Unhandled exception in Ocelot gateway");
}
finally
{
	Log.Information("Shut down Ocelot Api Gateway complete");
	Log.CloseAndFlush();
}

