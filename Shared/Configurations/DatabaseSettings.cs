namespace Shared.Configurations;

public sealed class DatabaseSettings
{
	public const string SectionName = "DatabaseSettings";

	public string Provider { get; set; } = string.Empty;

	public string ConnectionString { get; set; } = string.Empty;
}
