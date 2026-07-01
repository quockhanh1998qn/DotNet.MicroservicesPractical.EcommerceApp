using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Configurations;

namespace HangFire.API.Services;

/// <summary>
/// SMTP email sender backed by MailKit. Falls back to logging-only mode when no host is configured.
/// </summary>
public sealed class SmtpEmailService : ISmtpEmailService
{
	private readonly EmailSettings _settings;
	private readonly ILogger<SmtpEmailService> _logger;

	public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
	{
		_settings = options.Value;
		_logger = logger;
	}

	public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_settings.Host))
		{
			_logger.LogInformation("[Email-Stub] No SMTP host configured. To={To} Subject={Subject}", message.To, message.Subject);
			return;
		}

		var mime = new MimeMessage();
		mime.From.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(_settings.FromAddress) ? _settings.UserName : _settings.FromAddress));
		mime.To.Add(MailboxAddress.Parse(message.To));
		mime.Subject = message.Subject;
		mime.Body = new TextPart("plain") { Text = message.Body };

		using var client = new SmtpClient();
		await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
		if (!string.IsNullOrWhiteSpace(_settings.UserName))
		{
			await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
		}

		await client.SendAsync(mime, cancellationToken);
		await client.DisconnectAsync(quit: true, cancellationToken);

		_logger.LogInformation("Email sent. To={To} Subject={Subject}", message.To, message.Subject);
	}
}
