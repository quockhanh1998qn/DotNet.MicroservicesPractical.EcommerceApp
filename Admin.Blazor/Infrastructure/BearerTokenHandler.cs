using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Admin.Blazor.Infrastructure;

public class BearerTokenHandler : DelegatingHandler
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var ctx = _httpContextAccessor.HttpContext;
		if (ctx is not null)
		{
			var token = await ctx.GetTokenAsync("access_token");
			if (!string.IsNullOrEmpty(token))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}
		}
		return await base.SendAsync(request, cancellationToken);
	}
}
