using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Common.Logging.CorrelationId;

/// <summary>
/// Ensures every request has an <c>X-Correlation-Id</c> header and pushes it into Serilog's
/// <see cref="LogContext"/> as <c>CorrelationId</c> for downstream enrichment.
/// </summary>
public sealed class CorrelationIdMiddleware
{
	public const string HeaderName = "X-Correlation-Id";
	public const string LogPropertyName = "CorrelationId";

	private readonly RequestDelegate _next;

	public CorrelationIdMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		var correlationId = ResolveCorrelationId(context);
		context.Items[LogPropertyName] = correlationId;

		context.Response.OnStarting(() =>
		{
			if (!context.Response.Headers.ContainsKey(HeaderName))
			{
				context.Response.Headers[HeaderName] = correlationId;
			}
			return Task.CompletedTask;
		});

		using (LogContext.PushProperty(LogPropertyName, correlationId))
		{
			await _next(context);
		}
	}

	private static string ResolveCorrelationId(HttpContext context)
	{
		if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
		{
			var fromHeader = headerValue.ToString();
			if (!string.IsNullOrWhiteSpace(fromHeader))
			{
				return fromHeader;
			}
		}

		return Guid.NewGuid().ToString("N");
	}
}

public static class CorrelationIdMiddlewareExtensions
{
	public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
		=> app.UseMiddleware<CorrelationIdMiddleware>();
}
