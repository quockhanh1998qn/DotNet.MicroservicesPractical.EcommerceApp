using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared.Configurations;

namespace Identity.Server.Services;

public interface IEmailService
{
	Task SendEmailAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public class EmailService : IEmailService
{
	private readonly EmailSettings _settings;
	private readonly ILogger<EmailService> _logger;

	public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
	{
		_settings = settings.Value;
		_logger = logger;
	}

	public async Task SendEmailAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_settings.Host))
		{
			_logger.LogInformation("[email-stub] To={To} Subject={Subject}\n{Body}", toAddress, subject, htmlBody);
			return;
		}

		var message = new MimeMessage();
		message.From.Add(MailboxAddress.Parse(_settings.FromAddress ?? "no-reply@tedu.local"));
		message.To.Add(MailboxAddress.Parse(toAddress));
		message.Subject = subject;
		message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

		using var client = new SmtpClient();
		await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
		if (!string.IsNullOrWhiteSpace(_settings.UserName))
		{
			await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
		}
		await client.SendAsync(message, cancellationToken);
		await client.DisconnectAsync(true, cancellationToken);
	}
}
