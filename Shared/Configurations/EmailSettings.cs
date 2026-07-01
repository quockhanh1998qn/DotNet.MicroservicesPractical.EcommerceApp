namespace Shared.Configurations;

public sealed class EmailSettings
{
	public const string SectionName = "EmailSettings";

	public string Host { get; set; } = string.Empty;

	public int Port { get; set; }

	public string UserName { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;

	public string FromAddress { get; set; } = string.Empty;
}
