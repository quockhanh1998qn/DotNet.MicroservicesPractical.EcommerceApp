using Microsoft.Extensions.Logging;
using Ordering.Application.Common.Interfaces;

namespace Ordering.Infrastructure.Services;

/// <summary>
/// Stub SMTP service. Real Google SMTP integration is added later (Wave 8/Hangfire).
/// </summary>
public class SmtpEmailService : ISmtpEmailService
{
	private readonly ILogger<SmtpEmailService> _logger;

	public SmtpEmailService(ILogger<SmtpEmailService> logger)
	{
		_logger = logger;
	}

	public Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("[Email-Stub] To={To} Subject={Subject}", message.To, message.Subject);
		return Task.CompletedTask;
	}
}
