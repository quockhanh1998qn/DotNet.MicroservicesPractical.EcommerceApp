using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging.CorrelationId;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Common.Logging.UnitTests;

public class CorrelationIdMiddlewareTests
{
	[Fact]
	public async Task GeneratesCorrelationId_WhenRequestHasNoHeader()
	{
		var context = BuildContext();
		var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

		await middleware.InvokeAsync(context);
		await FireOnStartingAsync(context);

		var stored = context.Items[CorrelationIdMiddleware.LogPropertyName] as string;
		Assert.False(string.IsNullOrWhiteSpace(stored));
		Assert.Equal(stored, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
	}

	[Fact]
	public async Task PropagatesInboundCorrelationId_WhenHeaderPresent()
	{
		const string inbound = "abc-123-def";
		var context = BuildContext();
		context.Request.Headers[CorrelationIdMiddleware.HeaderName] = inbound;
		var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

		await middleware.InvokeAsync(context);
		await FireOnStartingAsync(context);

		Assert.Equal(inbound, context.Items[CorrelationIdMiddleware.LogPropertyName]);
		Assert.Equal(inbound, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
	}

	[Fact]
	public async Task DoesNotOverwriteExistingResponseHeader()
	{
		var context = BuildContext();
		context.Response.Headers[CorrelationIdMiddleware.HeaderName] = "preexisting";
		var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

		await middleware.InvokeAsync(context);
		await FireOnStartingAsync(context);

		Assert.Equal("preexisting", context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
	}

	private static DefaultHttpContext BuildContext()
	{
		var context = new DefaultHttpContext();
		context.Features.Set<IHttpResponseFeature>(new CapturingResponseFeature());
		return context;
	}

	private static async Task FireOnStartingAsync(HttpContext context)
	{
		if (context.Features.Get<IHttpResponseFeature>() is CapturingResponseFeature feature)
		{
			await feature.FireOnStartingAsync();
		}
	}

	private sealed class CapturingResponseFeature : IHttpResponseFeature
	{
		private readonly List<(Func<object, Task> Callback, object State)> _onStarting = new();

		public int StatusCode { get; set; } = 200;
		public string? ReasonPhrase { get; set; }
		public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
		public System.IO.Stream Body { get; set; } = System.IO.Stream.Null;
		public bool HasStarted { get; private set; }

		public void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));
		public void OnCompleted(Func<object, Task> callback, object state) { }

		public async Task FireOnStartingAsync()
		{
			HasStarted = true;
			foreach (var (cb, st) in _onStarting) await cb(st);
		}
	}
}
