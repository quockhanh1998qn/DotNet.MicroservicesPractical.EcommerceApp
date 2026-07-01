namespace Shared.Configurations;

public sealed class HangfireSettings
{
	public const string SectionName = "HangfireSettings";

	public string Route { get; set; } = "/hangfire";

	public string DashboardTitle { get; set; } = "Background Jobs Dashboard";

	public StorageSettings Storage { get; set; } = new();
}

public sealed class StorageSettings
{
	public string ConnectionString { get; set; } = string.Empty;
}

public sealed class BasketReminderSettings
{
	public const string SectionName = "BasketReminderSettings";

	public string BasketApiBaseUrl { get; set; } = string.Empty;

	public string CustomerApiBaseUrl { get; set; } = string.Empty;

	public int ReminderDelayMinutes { get; set; } = 10;
}
