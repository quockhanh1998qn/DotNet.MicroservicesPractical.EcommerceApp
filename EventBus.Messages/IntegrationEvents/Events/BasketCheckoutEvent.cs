namespace EventBus.Messages.IntegrationEvents.Events;

public class BasketCheckoutEvent : IntegrationBaseEvent
{
	public string UserName { get; set; } = string.Empty;
	public decimal TotalPrice { get; set; }

	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string EmailAddress { get; set; } = string.Empty;

	public string ShippingAddress { get; set; } = string.Empty;
	public string InvoiceAddress { get; set; } = string.Empty;
}

public interface IBasketCheckoutEvent
{
	Guid Id { get; }
	DateTime CreationDate { get; }
	string UserName { get; }
	decimal TotalPrice { get; }
	string FirstName { get; }
	string LastName { get; }
	string EmailAddress { get; }
	string ShippingAddress { get; }
	string InvoiceAddress { get; }
}
