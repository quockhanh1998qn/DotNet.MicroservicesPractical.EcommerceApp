using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Auth;

/// <summary>
/// One-stop extension to register JWT bearer authentication + per-scope
/// authorization policies. Reads <see cref="AuthSettings"/> from the
/// <c>AuthSettings</c> configuration section.
/// </summary>
public static class AuthExtensions
{
	public const string ScopePolicyPrefix = "scope:";

	public static IServiceCollection AddMicroserviceAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		var settings = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();
		services.AddSingleton(settings);

		if (string.IsNullOrWhiteSpace(settings.Authority))
		{
			// Auth not yet configured (e.g. during local DB bootstrap) — register
			// stub authentication so [Authorize] still resolves without failing DI.
			services.AddAuthentication();
			services.AddAuthorization();
			return services;
		}

		services
			.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
			{
				options.Authority = settings.Authority;
				options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
				options.Audience = settings.Audience;
				options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(settings.Audience);
			});

		services.AddAuthorization(options =>
		{
			if (!string.IsNullOrWhiteSpace(settings.RequiredScope))
			{
				var policyName = $"{ScopePolicyPrefix}{settings.RequiredScope}";
				options.AddPolicy(policyName, policy =>
				{
					policy.RequireAuthenticatedUser();
					policy.RequireClaim("scope", settings.RequiredScope);
				});
				options.DefaultPolicy = options.GetPolicy(policyName)!;
			}
		});

		return services;
	}

	/// <summary>
	/// Registers an authorization policy named <c>permission:{function}:{command}</c>
	/// (e.g. <c>permission:product:Write</c>) that requires the matching claim
	/// to be present on the access token.
	/// </summary>
	public static AuthorizationOptions AddPermissionPolicy(this AuthorizationOptions options, string function, string command)
	{
		options.AddPolicy($"permission:{function}:{command}", policy =>
		{
			policy.RequireAuthenticatedUser();
			policy.RequireClaim("permission", $"{function}:{command}");
		});
		return options;
	}
}
