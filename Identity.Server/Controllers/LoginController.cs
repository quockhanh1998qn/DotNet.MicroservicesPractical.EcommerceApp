using Duende.IdentityServer.Services;
using Identity.Server.Entities;
using Identity.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Server.Controllers;

[AllowAnonymous]
[Route("Account")]
public class LoginController : Controller
{
	private readonly SignInManager<User> _signInManager;
	private readonly UserManager<User> _userManager;
	private readonly IIdentityServerInteractionService _interaction;
	private readonly ILogger<LoginController> _logger;

	public LoginController(
		SignInManager<User> signInManager,
		UserManager<User> userManager,
		IIdentityServerInteractionService interaction,
		ILogger<LoginController> logger)
	{
		_signInManager = signInManager;
		_userManager = userManager;
		_interaction = interaction;
		_logger = logger;
	}

	[HttpGet("Login")]
	public IActionResult Login(string? returnUrl = null)
	{
		return View(new LoginViewModel { ReturnUrl = returnUrl });
	}

	[HttpPost("Login")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var user = await _userManager.FindByEmailAsync(model.Email);
		if (user is null)
		{
			ModelState.AddModelError(string.Empty, "Invalid credentials.");
			return View(model);
		}

		var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
		if (!result.Succeeded)
		{
			_logger.LogWarning("Failed login attempt for {Email}", model.Email);
			ModelState.AddModelError(string.Empty, "Invalid credentials.");
			return View(model);
		}

		if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
		{
			return Redirect(model.ReturnUrl);
		}

		return Redirect("~/");
	}

	[HttpGet("Logout")]
	public async Task<IActionResult> Logout(string? logoutId = null)
	{
		await _signInManager.SignOutAsync();
		var context = logoutId is null ? null : await _interaction.GetLogoutContextAsync(logoutId);
		if (!string.IsNullOrEmpty(context?.PostLogoutRedirectUri))
		{
			return Redirect(context.PostLogoutRedirectUri);
		}
		return Redirect("~/");
	}
}
