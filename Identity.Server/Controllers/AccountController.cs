using Identity.Server.Entities;
using Identity.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Identity.Server.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
	private readonly UserManager<User> _userManager;
	private readonly IEmailService _emailService;
	private readonly ILogger<AccountController> _logger;

	public AccountController(
		UserManager<User> userManager,
		IEmailService emailService,
		ILogger<AccountController> logger)
	{
		_userManager = userManager;
		_emailService = emailService;
		_logger = logger;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
	{
		var user = new User
		{
			UserName = request.Email,
			Email = request.Email,
			FirstName = request.FirstName,
			LastName = request.LastName,
		};

		var result = await _userManager.CreateAsync(user, request.Password);
		if (!result.Succeeded)
		{
			return BadRequest(result.Errors.Select(e => new { e.Code, e.Description }));
		}

		await _userManager.AddToRoleAsync(user, "Customer");
		var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
		var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
		var confirmUrl = $"{Request.Scheme}://{Request.Host}/api/account/confirm-email?userId={user.Id}&token={encoded}";

		await _emailService.SendEmailAsync(
			user.Email!,
			"Confirm your Tedu account",
			$"<p>Hi {user.FirstName},</p><p>Please confirm your email by clicking <a href=\"{confirmUrl}\">here</a>.</p>",
			cancellationToken);

		return Ok(new { user.Id, user.Email, message = "Confirmation email sent." });
	}

	[HttpGet("confirm-email")]
	public async Task<IActionResult> ConfirmEmail([FromQuery] long userId, [FromQuery] string token)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString());
		if (user is null) return NotFound();

		var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
		var result = await _userManager.ConfirmEmailAsync(user, decoded);
		return result.Succeeded ? Ok(new { confirmed = true }) : BadRequest(result.Errors);
	}

	[HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
	{
		var user = await _userManager.FindByEmailAsync(request.Email);
		if (user is null)
		{
			// Do not reveal whether the email is registered.
			return Ok(new { message = "If the email exists, a reset link has been sent." });
		}

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
		var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?userId={user.Id}&token={encoded}";

		await _emailService.SendEmailAsync(
			user.Email!,
			"Reset your Tedu password",
			$"<p>Reset your password by clicking <a href=\"{resetUrl}\">here</a>. The link expires in 1 hour.</p>",
			cancellationToken);

		return Ok(new { message = "If the email exists, a reset link has been sent." });
	}

	[HttpPost("reset-password")]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
	{
		var user = await _userManager.FindByIdAsync(request.UserId.ToString());
		if (user is null) return NotFound();

		var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
		var result = await _userManager.ResetPasswordAsync(user, decoded, request.NewPassword);
		return result.Succeeded ? Ok(new { reset = true }) : BadRequest(result.Errors);
	}
}

public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(long UserId, string Token, string NewPassword);
