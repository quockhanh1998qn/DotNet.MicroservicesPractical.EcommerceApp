using System.Security.Claims;
using Identity.Server.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Server.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
	private readonly IPermissionRepository _permissions;

	public PermissionsController(IPermissionRepository permissions)
	{
		_permissions = permissions;
	}

	/// <summary>
	/// Returns the (function, command) permission set granted to the
	/// currently authenticated user via their roles.
	/// </summary>
	[HttpGet("me")]
	public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
	{
		var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		if (!long.TryParse(subject, out var userId))
		{
			return Unauthorized();
		}

		var permissions = await _permissions.GetPermissionsByUserAsync(userId, cancellationToken);
		return Ok(permissions.Select(p => new { p.Function, p.Command }));
	}
}
