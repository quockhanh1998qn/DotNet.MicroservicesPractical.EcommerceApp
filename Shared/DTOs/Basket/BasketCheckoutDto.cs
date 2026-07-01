using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Basket;

public class BasketCheckoutDto
{
	[Required]
	[StringLength(100, MinimumLength = 1)]
	public string UserName { get; set; } = string.Empty;

	public decimal TotalPrice { get; set; }

	[Required]
	[StringLength(100, MinimumLength = 1)]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[StringLength(100, MinimumLength = 1)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string EmailAddress { get; set; } = string.Empty;

	[Required]
	[StringLength(500)]
	public string ShippingAddress { get; set; } = string.Empty;

	[Required]
	[StringLength(500)]
	public string InvoiceAddress { get; set; } = string.Empty;
}
