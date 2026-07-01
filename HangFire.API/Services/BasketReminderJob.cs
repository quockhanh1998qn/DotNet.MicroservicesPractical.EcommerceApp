using System.Net;
using System.Net.Http.Json;
using Shared.DTOs.Basket;

namespace HangFire.API.Services;

/// <summary>
/// Hangfire job target: checks Basket.API for an existing basket and emails a reminder if found.
/// </summary>
public sealed class BasketReminderJob : IBasketReminderJob
{
	public const string BasketApiClientName = "BasketApi";

	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ISmtpEmailService _emailService;
	private readonly ILogger<BasketReminderJob> _logger;

	public BasketReminderJob(
		IHttpClientFactory httpClientFactory,
		ISmtpEmailService emailService,
		ILogger<BasketReminderJob> logger)
	{
		_httpClientFactory = httpClientFactory;
		_emailService = emailService;
		_logger = logger;
	}

	public async Task SendBasketReminderAsync(string username, string emailTo, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(username))
		{
			throw new ArgumentException("Username is required.", nameof(username));
		}

		if (string.IsNullOrWhiteSpace(emailTo))
		{
			throw new ArgumentException("Recipient email is required.", nameof(emailTo));
		}

		var client = _httpClientFactory.CreateClient(BasketApiClientName);
		var response = await client.GetAsync($"api/baskets/{Uri.EscapeDataString(username)}", cancellationToken);

		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			_logger.LogInformation("Basket for user {Username} already checked out. Skipping reminder.", username);
			return;
		}

		response.EnsureSuccessStatusCode();
		var cart = await response.Content.ReadFromJsonAsync<CartDto>(cancellationToken: cancellationToken);
		if (cart is null || cart.Items.Count == 0)
		{
			_logger.LogInformation("Basket for user {Username} is empty. Skipping reminder.", username);
			return;
		}

		var message = new EmailMessage
		{
			To = emailTo,
			Subject = "You still have items in your basket",
			Body = $"Hi {username},\n\n" +
				   $"You left {cart.Items.Count} item(s) in your basket totalling {cart.TotalPrice:C}.\n" +
				   "Complete your checkout to secure your order.\n",
		};

		await _emailService.SendEmailAsync(message, cancellationToken);
	}
}
