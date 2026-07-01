using System.Collections.Generic;
using Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Common.Auth.UnitTests;

public class AuthExtensionsTests
{
	[Fact]
	public void AddMicroserviceAuthentication_NoAuthority_RegistersStubsWithoutFailing()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

		services.AddMicroserviceAuthentication(config);
		var provider = services.BuildServiceProvider();

		var settings = provider.GetRequiredService<AuthSettings>();
		Assert.Equal(string.Empty, settings.Authority);

		// Authorization service registered even when authority missing.
		Assert.NotNull(provider.GetService<IAuthorizationService>());
	}

	[Fact]
	public void AddMicroserviceAuthentication_WithAuthorityAndScope_RegistersScopePolicy()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["AuthSettings:Authority"]     = "http://identity.local",
			["AuthSettings:Audience"]      = "tedu.microservices",
			["AuthSettings:RequiredScope"] = "product.api",
		}).Build();

		services.AddMicroserviceAuthentication(config);
		var provider = services.BuildServiceProvider();

		var settings = provider.GetRequiredService<AuthSettings>();
		Assert.Equal("http://identity.local", settings.Authority);
		Assert.Equal("product.api", settings.RequiredScope);

		var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
		var policy = options.GetPolicy($"{AuthExtensions.ScopePolicyPrefix}product.api");
		Assert.NotNull(policy);
		Assert.Equal(policy, options.DefaultPolicy);
	}

	[Fact]
	public void AddPermissionPolicy_RegistersClaimRequirement()
	{
		var options = new AuthorizationOptions();
		options.AddPermissionPolicy("product", "Write");

		var policy = options.GetPolicy("permission:product:Write");
		Assert.NotNull(policy);
		Assert.Contains(policy!.Requirements, r => r is ClaimsAuthorizationRequirement c
			&& c.ClaimType == "permission");
	}
}
