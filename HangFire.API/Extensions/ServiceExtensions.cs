using Common.Auth;
using HangFire.API.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Extensions.Http;
using Shared.Configurations;

namespace HangFire.API.Extensions;

public static class ServiceExtensions
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddControllers();
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen();

		services.AddOptions<HangfireSettings>()
			.Bind(configuration.GetSection(HangfireSettings.SectionName));
		services.AddOptions<EmailSettings>()
			.Bind(configuration.GetSection(EmailSettings.SectionName));
		services.AddOptions<BasketReminderSettings>()
			.Bind(configuration.GetSection(BasketReminderSettings.SectionName));

		services.AddHangfireServices(configuration);
		services.AddJobDependencies(configuration);

		var storageConnection = configuration["HangfireSettings:Storage:ConnectionString"] ?? string.Empty;
		services.AddHealthChecks()
			.AddSqlServer(storageConnection, name: "hangfire-sqlserver", tags: new[] { "ready", "db" })
			.AddHangfire(opt => opt.MinimumAvailableServers = 1, name: "hangfire", tags: new[] { "ready", "jobs" });

		services.AddMicroserviceAuthentication(configuration);

		return services;
	}

	private static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
	{
		var hangfireSettings = configuration
			.GetSection(HangfireSettings.SectionName)
			.Get<HangfireSettings>() ?? new HangfireSettings();

		var connectionString = hangfireSettings.Storage.ConnectionString;
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException("HangfireSettings:Storage:ConnectionString is missing.");
		}

		EnsureDatabaseExists(connectionString);

		services.AddHangfire(globalConfiguration =>
		{
			globalConfiguration
				.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseSqlServerStorage(connectionString, new SqlServerStorageOptions
				{
					CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
					SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
					QueuePollInterval = TimeSpan.Zero,
					UseRecommendedIsolationLevel = true,
					DisableGlobalLocks = true,
				});
		});

		services.AddHangfireServer();

		return services;
	}

	private static IServiceCollection AddJobDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		var reminder = configuration
			.GetSection(BasketReminderSettings.SectionName)
			.Get<BasketReminderSettings>() ?? new BasketReminderSettings();

		services.AddHttpClient(BasketReminderJob.BasketApiClientName, client =>
		{
			if (!string.IsNullOrWhiteSpace(reminder.BasketApiBaseUrl))
			{
				client.BaseAddress = new Uri(reminder.BasketApiBaseUrl, UriKind.Absolute);
			}
			client.Timeout = TimeSpan.FromSeconds(15);
		})
		.AddPolicyHandler(HttpPolicyExtensions
			.HandleTransientHttpError()
			.WaitAndRetryAsync(retryCount: 3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
		.AddPolicyHandler(HttpPolicyExtensions
			.HandleTransientHttpError()
			.CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30)))
		.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

		services.AddScoped<ISmtpEmailService, SmtpEmailService>();
		services.AddScoped<IBasketReminderJob, BasketReminderJob>();

		return services;
	}

	private static void EnsureDatabaseExists(string connectionString)
	{
		var builder = new SqlConnectionStringBuilder(connectionString);
		var databaseName = builder.InitialCatalog;
		if (string.IsNullOrWhiteSpace(databaseName))
		{
			return;
		}

		builder.InitialCatalog = "master";

		using var connection = new SqlConnection(builder.ConnectionString);
		connection.Open();
		using var command = connection.CreateCommand();
		command.CommandText = $"IF DB_ID(@name) IS NULL CREATE DATABASE [{databaseName.Replace("]", "]]")}]";
		command.Parameters.Add(new SqlParameter("@name", databaseName));
		command.ExecuteNonQuery();
	}
}
