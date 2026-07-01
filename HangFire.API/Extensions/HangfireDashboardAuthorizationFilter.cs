using Hangfire.Dashboard;

namespace HangFire.API.Extensions;

/// <summary>
/// Permissive filter for development. Replace with role-based filter after IdentityServer is wired (Wave 10).
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
	public bool Authorize(DashboardContext context) => true;
}
