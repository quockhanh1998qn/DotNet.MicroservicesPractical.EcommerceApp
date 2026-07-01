using Admin.Blazor.Components;
using Admin.Blazor.Infrastructure;
using Admin.Blazor.Services;
using Common.Logging;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(Serilogger.Configure);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOnly", p => p.RequireAuthenticatedUser().RequireRole("Admin"));
	options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services
	.AddAuthentication(options =>
	{
		options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
	})
	.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
	{
		o.Cookie.SameSite = SameSiteMode.Lax;
		o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
	})
	.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
	{
		options.Authority = builder.Configuration["IdentityServer:Authority"] ?? "http://localhost:5009";
		options.ClientId = "admin_blazor";
		options.ClientSecret = "blazor-admin-secret";
		options.ResponseType = "code";
		options.UsePkce = true;
		options.SaveTokens = true;
		options.GetClaimsFromUserInfoEndpoint = true;
		options.RequireHttpsMetadata = false;
		options.MapInboundClaims = false;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			NameClaimType = "name",
			RoleClaimType = "role",
		};
		options.Scope.Clear();
		options.Scope.Add("openid");
		options.Scope.Add("profile");
		options.Scope.Add("email");
		options.Scope.Add("roles");
		options.Scope.Add("offline_access");
		foreach (var s in new[] { "product.api", "customer.api", "basket.api", "ordering.api", "inventory.api" })
		{
			options.Scope.Add(s);
		}
		options.NonceCookie.SameSite = SameSiteMode.Lax;
		options.CorrelationCookie.SameSite = SameSiteMode.Lax;

		// IdentityServer emits multi-valued "role" claim. Map JSON array into multiple Claims.
		options.ClaimActions.MapJsonKey("role", "role", "role");
		options.Events.OnTokenValidated = ctx =>
		{
			if (ctx.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt && ctx.Principal?.Identity is System.Security.Claims.ClaimsIdentity id)
			{
				foreach (var c in jwt.Claims.Where(c => c.Type == "role"))
				{
					if (!id.HasClaim("role", c.Value)) id.AddClaim(new System.Security.Claims.Claim("role", c.Value));
				}
			}
			return Task.CompletedTask;
		};
		options.Events.OnUserInformationReceived = ctx =>
		{
			if (ctx.Principal?.Identity is not System.Security.Claims.ClaimsIdentity id) return Task.CompletedTask;
			if (ctx.User.RootElement.TryGetProperty("role", out var roleEl))
			{
				if (roleEl.ValueKind == System.Text.Json.JsonValueKind.Array)
				{
					foreach (var v in roleEl.EnumerateArray())
					{
						var s = v.GetString();
						if (!string.IsNullOrEmpty(s) && !id.HasClaim("role", s)) id.AddClaim(new System.Security.Claims.Claim("role", s));
					}
				}
				else if (roleEl.ValueKind == System.Text.Json.JsonValueKind.String)
				{
					var s = roleEl.GetString();
					if (!string.IsNullOrEmpty(s) && !id.HasClaim("role", s)) id.AddClaim(new System.Security.Claims.Claim("role", s));
				}
			}
			return Task.CompletedTask;
		};
	});

builder.Services.AddTransient<BearerTokenHandler>();

var apiBase = builder.Configuration["Gateway:BaseUrl"] ?? "http://localhost:5000";
var gatewayUri = new Uri(apiBase.TrimEnd('/') + "/");
void AddApi<T>() where T : class
{
	builder.Services.AddHttpClient<T>(c => c.BaseAddress = gatewayUri)
		.AddHttpMessageHandler<BearerTokenHandler>()
		.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, n => TimeSpan.FromMilliseconds(200 * Math.Pow(2, n))));
}
AddApi<ProductApiClient>();
AddApi<CustomerApiClient>();
AddApi<OrderApiClient>();
AddApi<InventoryApiClient>();

builder.Services.AddSingleton<StockUpdateBroadcaster>();
builder.Services.AddMassTransit(x =>
{
	x.UsingRabbitMq((ctx, cfg) =>
	{
		cfg.Host(builder.Configuration["EventBus:Host"] ?? "amqp://guest:guest@localhost:5672");
		cfg.ReceiveEndpoint("admin-blazor-stock-updates", e =>
		{
			e.Consumer(() => new StockUpdateConsumer(ctx.GetRequiredService<StockUpdateBroadcaster>()));
		});
	});
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/account/login", async (HttpContext http, string? returnUrl) =>
{
	await http.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
	{
		RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
	});
}).AllowAnonymous();

app.MapGet("/account/logout", async (HttpContext http) =>
{
	await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	await http.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
});

app.MapGet("/Account/AccessDenied", (HttpContext http) =>
{
	http.Response.StatusCode = 403;
	var user = http.User?.Identity?.Name ?? "(unknown)";
	var html = $$"""
		<!doctype html><html><head><title>Access denied</title>
		<style>body{font-family:Segoe UI,sans-serif;max-width:560px;margin:8vh auto;color:#111}.box{padding:2rem;border:1px solid #e5e7eb;border-radius:12px;box-shadow:0 4px 24px rgba(0,0,0,.06)}h1{margin:0 0 .5rem;color:#b91c1c}code{background:#f3f4f6;padding:1px 6px;border-radius:4px}a{color:#4f46e5}</style>
		</head><body><div class="box">
		<h1>Access denied</h1>
		<p>You are signed in as <code>{{user}}</code> but you do not have the <b>Admin</b> role required to use this portal.</p>
		<p>Sign in with <code>admin@tedu.local</code> / <code>Admin@123!</code>.</p>
		<p><a href="/account/logout">Sign out</a> &middot; <a href="/">Home</a></p>
		</div></body></html>
		""";
	return Results.Content(html, "text/html");
}).AllowAnonymous();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
