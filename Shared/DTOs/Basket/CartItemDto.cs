using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Basket;

public class CartItemDto
{
	[Required]
	[StringLength(64, MinimumLength = 1)]
	public string ProductNo { get; set; } = string.Empty;

	[Required]
	[StringLength(200, MinimumLength = 1)]
	public string ProductName { get; set; } = string.Empty;

	[Range(1, int.MaxValue)]
	public int Quantity { get; set; }

	[Range(0.0, double.MaxValue)]
	public decimal Price { get; set; }
}
