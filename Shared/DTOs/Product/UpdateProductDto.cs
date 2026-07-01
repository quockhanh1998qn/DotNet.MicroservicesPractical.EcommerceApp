using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Product;

public class UpdateProductDto
{
	[Required]
	[StringLength(150, MinimumLength = 2)]
	public string No { get; set; } = string.Empty;

	[Required]
	[StringLength(250, MinimumLength = 2)]
	public string Name { get; set; } = string.Empty;

	[Required]
	[StringLength(255, MinimumLength = 2)]
	public string Summary { get; set; } = string.Empty;

	[StringLength(4000)]
	public string Description { get; set; } = string.Empty;

	[Range(0.01, 999999999.99)]
	public decimal Price { get; set; }
}
