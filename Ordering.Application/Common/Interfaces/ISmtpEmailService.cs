namespace Ordering.Application.Common.Interfaces;

public class EmailMessage
{
	public string To { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Body { get; set; } = string.Empty;
}

public interface ISmtpEmailService
{
	Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
