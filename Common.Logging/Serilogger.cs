using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Common.Logging;

public static class Serilogger
{
	public static Action<HostBuilderContext, LoggerConfiguration> Configure =>
		(context, configuration) =>
		{
			var applicationName = context.HostingEnvironment.ApplicationName?.ToLower().Replace(".", "-");
			var environmentName = context.HostingEnvironment.EnvironmentName ?? "Development";
			var logFilePath = $"logs/{applicationName}-.log";

			configuration
				.WriteTo.Debug()
				.WriteTo.Console(outputTemplate:
				"[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
				.WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
				.Enrich.FromLogContext()
				.Enrich.WithMachineName()
				.Enrich.WithProperty("Environment", environmentName)
				.Enrich.WithProperty("Application", applicationName);

			AddElasticsearchSink(configuration, context.Configuration, applicationName, environmentName);

			configuration.ReadFrom.Configuration(context.Configuration);
		};

	private static void AddElasticsearchSink(
		LoggerConfiguration configuration,
		IConfiguration appConfiguration,
		string? applicationName,
		string environmentName)
	{
		var elasticUri = appConfiguration["ElasticConfiguration:Uri"];
		if (string.IsNullOrWhiteSpace(elasticUri))
		{
			return;
		}

		var indexFormat = $"tedu-microservices-{applicationName ?? "app"}-{environmentName.ToLower()}-{DateTime.UtcNow:yyyy-MM}";
		configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
		{
			AutoRegisterTemplate = true,
			AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
			IndexFormat = indexFormat,
			MinimumLogEventLevel = Serilog.Events.LogEventLevel.Information,
		});
	}
}
