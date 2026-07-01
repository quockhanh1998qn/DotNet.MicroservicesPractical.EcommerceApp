using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Basket;

public class CartDto
{
	[Required]
	[StringLength(100, MinimumLength = 1)]
	public string Username { get; set; } = string.Empty;

	public List<CartItemDto> Items { get; set; } = new();

	public decimal TotalPrice => Items?.Sum(i => i.Price * i.Quantity) ?? 0m;
}
