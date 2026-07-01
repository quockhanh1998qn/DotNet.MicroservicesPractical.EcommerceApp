using EventBus.Messages.IntegrationEvents.Events;
using MassTransit;

namespace Admin.Blazor.Services;

public class StockUpdateBroadcaster
{
	public event Func<StockUpdatedEvent, Task>? StockUpdated;

	public async Task RaiseAsync(StockUpdatedEvent payload)
	{
		if (StockUpdated is null) return;
		foreach (var handler in StockUpdated.GetInvocationList().Cast<Func<StockUpdatedEvent, Task>>())
		{
			try { await handler(payload); } catch { /* swallow per-subscriber errors */ }
		}
	}
}

public class StockUpdateConsumer : IConsumer<StockUpdatedEvent>
{
	private readonly StockUpdateBroadcaster _broadcaster;
	public StockUpdateConsumer(StockUpdateBroadcaster broadcaster) => _broadcaster = broadcaster;

	public Task Consume(ConsumeContext<StockUpdatedEvent> context) => _broadcaster.RaiseAsync(context.Message);
}
