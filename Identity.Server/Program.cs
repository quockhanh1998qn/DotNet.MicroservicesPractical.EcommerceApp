using System.Reflection;
using Common.Logging;
using Common.Logging.CorrelationId;
using Identity.Server.Entities;
using Identity.Server.Persistence;
using Identity.Server.Repositories;
using Identity.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);
Log.Information("Start Identity Server up");

try
{
	var connectionString = builder.Configuration.GetConnectionString("IdentitySqlConnection")
		?? throw new InvalidOperationException("ConnectionStrings:IdentitySqlConnection is missing.");
	var migrationsAssembly = typeof(Program).GetTypeInfo().Assembly.GetName().Name;

	builder.Services.AddControllersWithViews();
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();

	builder.Services.AddDbContext<IdentityContext>(o =>
		o.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

	builder.Services.AddIdentity<User, Role>(options =>
		{
			options.Password.RequireDigit = true;
			options.Password.RequireUppercase = true;
			options.Password.RequiredLength = 8;
			options.User.RequireUniqueEmail = true;
			options.SignIn.RequireConfirmedEmail = false; // dev convenience
		})
		.AddEntityFrameworkStores<IdentityContext>()
		.AddDefaultTokenProviders();

	builder.Services
		.AddIdentityServer(options =>
		{
			options.Events.RaiseErrorEvents = true;
			options.Events.RaiseFailureEvents = true;
			options.Events.RaiseInformationEvents = true;
			options.Events.RaiseSuccessEvents = true;
			options.EmitStaticAudienceClaim = true;
			options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
			// Pin the issuer so tokens issued through the public URL (browser via http://localhost:5009)
			// are still accepted by APIs that load discovery through the docker DNS name
			// (http://identity.server:8080). Without this, `iss` differs and JWT validation fails with 401.
			var issuer = builder.Configuration["IdentityServer:IssuerUri"];
			if (!string.IsNullOrWhiteSpace(issuer))
			{
				options.IssuerUri = issuer;
			}
		})
		.AddConfigurationStore(o =>
			o.ConfigureDbContext = b =>
				b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)))
		.AddOperationalStore(o =>
		{
			o.ConfigureDbContext = b =>
				b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
			o.EnableTokenCleanup = true;
		})
		.AddAspNetIdentity<User>()
		.AddDeveloperSigningCredential();

	// Must come AFTER AddAspNetIdentity (which sets SameSite=None for silent renew).
	// Over plain HTTP localhost, SameSite=None requires Secure (rejected), so force Lax.
	builder.Services.ConfigureApplicationCookie(o =>
	{
		o.Cookie.SameSite = SameSiteMode.Lax;
		o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
	});
	builder.Services.ConfigureExternalCookie(o =>
	{
		o.Cookie.SameSite = SameSiteMode.Lax;
		o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
	});

	builder.Services.AddOptions<EmailSettings>().Bind(builder.Configuration.GetSection(EmailSettings.SectionName));
	builder.Services.AddScoped<IEmailService, EmailService>();
	builder.Services.AddScoped<IUserRepository, UserRepository>();
	builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();

	builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
		.SetIsOriginAllowed(_ => true)
		.AllowAnyHeader()
		.AllowAnyMethod()
		.AllowCredentials()));

	builder.Services.AddHealthChecks()
		.AddSqlServer(connectionString, name: "identitydb", tags: new[] { "ready", "db" });

	var app = builder.Build();

	app.UseCorrelationId();
	app.UseSerilogRequestLogging();

	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	app.UseStaticFiles();
	app.UseRouting();
	app.UseCors();
	app.UseIdentityServer();
	app.UseAuthentication();
	app.UseAuthorization();

	app.MapControllers();
	app.MapDefaultControllerRoute();
	app.MapHealthChecks("/health");

	using (var scope = app.Services.CreateScope())
	{
		var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
		try
		{
			await IdentityContextSeed.SeedAsync(scope.ServiceProvider, seedLogger);
			await PermissionDatabaseInitializer.EnsureCreatedAsync(connectionString);
		}
		catch (Exception seedEx)
		{
			seedLogger.LogError(seedEx, "Identity Server seed failed");
		}
	}

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Unhandled exception in Identity Server");
}
finally
{
	Log.Information("Shut down Identity Server complete");
	Log.CloseAndFlush();
}

public partial class Program { }
