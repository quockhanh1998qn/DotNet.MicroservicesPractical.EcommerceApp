using HangFire.API.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Configurations;

namespace HangFire.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RemindersController : ControllerBase
{
	private readonly IBackgroundJobClient _backgroundJobs;
	private readonly BasketReminderSettings _settings;

	public RemindersController(IBackgroundJobClient backgroundJobs, IOptions<BasketReminderSettings> settings)
	{
		_backgroundJobs = backgroundJobs;
		_settings = settings.Value;
	}

	public sealed record ScheduleBasketReminderRequest(string Username, string EmailTo, int? DelayMinutes);

	[HttpPost("baskets")]
	[ProducesResponseType(typeof(string), StatusCodes.Status202Accepted)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public IActionResult ScheduleBasketReminder([FromBody] ScheduleBasketReminderRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.EmailTo))
		{
			return BadRequest(new { message = "Username and EmailTo are required." });
		}

		var delayMinutes = request.DelayMinutes ?? _settings.ReminderDelayMinutes;
		var delay = TimeSpan.FromMinutes(Math.Max(delayMinutes, 0));

		var jobId = _backgroundJobs.Schedule<IBasketReminderJob>(
			j => j.SendBasketReminderAsync(request.Username, request.EmailTo, CancellationToken.None),
			delay);

		return Accepted(new { jobId, delayMinutes });
	}
}
