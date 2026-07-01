namespace HangFire.API.Services;

public interface IBasketReminderJob
{
	/// <summary>
	/// Sends a reminder email to <paramref name="username"/> if their basket is still pending (not checked out).
	/// </summary>
	Task SendBasketReminderAsync(string username, string emailTo, CancellationToken cancellationToken = default);
}
