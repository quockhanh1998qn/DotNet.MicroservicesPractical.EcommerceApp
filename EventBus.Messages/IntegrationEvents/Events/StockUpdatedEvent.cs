namespace EventBus.Messages.IntegrationEvents.Events;

public class StockUpdatedEvent : IntegrationBaseEvent
{
	public string ItemNo { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public string Reason { get; set; } = string.Empty;
}
