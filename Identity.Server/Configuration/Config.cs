using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Identity.Server.Configuration;

/// <summary>
/// Static seed data for IdentityServer: scopes, API resources, clients.
/// Loaded into the Configuration DB on first run.
/// </summary>
public static class Config
{
	public static IEnumerable<IdentityResource> IdentityResources =>
		new IdentityResource[]
		{
			new IdentityResources.OpenId(),
			new IdentityResources.Profile(),
			new IdentityResources.Email(),
			new IdentityResource("roles", "User roles", new[] { "role" }),
		};

	public static IEnumerable<ApiScope> ApiScopes =>
		new[]
		{
			new ApiScope("product.api",   "Product API"),
			new ApiScope("customer.api",  "Customer API"),
			new ApiScope("basket.api",    "Basket API"),
			new ApiScope("ordering.api",  "Ordering API"),
			new ApiScope("inventory.api", "Inventory API"),
			new ApiScope("hangfire.api",  "HangFire Dashboard"),
		};

	public static IEnumerable<ApiResource> ApiResources =>
		new[]
		{
			new ApiResource("tedu.microservices", "Tedu Microservices APIs")
			{
				Scopes = { "product.api", "customer.api", "basket.api", "ordering.api", "inventory.api", "hangfire.api" },
				UserClaims = { "role", "email", "given_name", "family_name" },
			},
		};

	public static IEnumerable<Client> Clients =>
		new[]
		{
			// Angular SPA: code + PKCE
			new Client
			{
				ClientId = "webapp_angular",
				ClientName = "Tedu Web App (Angular)",
				AllowedGrantTypes = GrantTypes.Code,
				RequirePkce = true,
				RequireClientSecret = false,
				AllowedCorsOrigins = { "http://localhost:4200" },
				RedirectUris = { "http://localhost:4200/auth-callback", "http://localhost:4200/silent-renew" },
				PostLogoutRedirectUris = { "http://localhost:4200/" },
				AllowedScopes =
				{
					IdentityServerConstants.StandardScopes.OpenId,
					IdentityServerConstants.StandardScopes.Profile,
					IdentityServerConstants.StandardScopes.Email,
					"roles",
					"product.api", "customer.api", "basket.api", "ordering.api", "inventory.api",
				},
				AccessTokenLifetime = 3600,
				AllowOfflineAccess = true,
				AlwaysIncludeUserClaimsInIdToken = true,
			},

			// Blazor Server admin: code + secret
			new Client
			{
				ClientId = "admin_blazor",
				ClientName = "Tedu Admin Portal (Blazor)",
				AllowedGrantTypes = GrantTypes.Code,
				RequirePkce = true,
				ClientSecrets = { new Secret("blazor-admin-secret".Sha256()) },
				RedirectUris = { "http://localhost:5100/signin-oidc" },
				PostLogoutRedirectUris = { "http://localhost:5100/signout-callback-oidc" },
				AllowedScopes =
				{
					IdentityServerConstants.StandardScopes.OpenId,
					IdentityServerConstants.StandardScopes.Profile,
					IdentityServerConstants.StandardScopes.Email,
					"roles",
					"product.api", "customer.api", "basket.api", "ordering.api", "inventory.api", "hangfire.api",
				},
				AccessTokenLifetime = 3600,
				AllowOfflineAccess = true,
			},

			// Service-to-service for HangFire jobs
			new Client
			{
				ClientId = "hangfire_worker",
				ClientName = "Hangfire background worker",
				AllowedGrantTypes = GrantTypes.ClientCredentials,
				ClientSecrets = { new Secret("hangfire-worker-secret".Sha256()) },
				AllowedScopes = { "basket.api", "customer.api" },
			},

			// Swagger UI testing client (development)
			new Client
			{
				ClientId = "swagger_ui",
				ClientName = "Swagger UI",
				AllowedGrantTypes = GrantTypes.Code,
				RequirePkce = true,
				RequireClientSecret = false,
				RedirectUris =
				{
					"http://localhost:5102/swagger/oauth2-redirect.html",
					"http://localhost:5103/swagger/oauth2-redirect.html",
					"http://localhost:5004/swagger/oauth2-redirect.html",
					"http://localhost:5005/swagger/oauth2-redirect.html",
					"http://localhost:5006/swagger/oauth2-redirect.html",
					"http://localhost:5007/swagger/oauth2-redirect.html",
				},
				AllowedCorsOrigins = { "http://localhost:5102", "http://localhost:5103", "http://localhost:5004", "http://localhost:5005", "http://localhost:5006", "http://localhost:5007" },
				AllowedScopes =
				{
					IdentityServerConstants.StandardScopes.OpenId,
					IdentityServerConstants.StandardScopes.Profile,
					"roles",
					"product.api", "customer.api", "basket.api", "ordering.api", "inventory.api", "hangfire.api",
				},
			},
		};
}
