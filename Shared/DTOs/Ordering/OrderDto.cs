namespace Shared.DTOs.Ordering;

public class OrderDto
{
	public long Id { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string EmailAddress { get; set; } = string.Empty;
	public string ShippingAddress { get; set; } = string.Empty;
	public string InvoiceAddress { get; set; } = string.Empty;
	public decimal TotalPrice { get; set; }
	public int Status { get; set; }
}
