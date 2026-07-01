namespace Shared.DTOs.Product;

public class ProductDto
{
	public long Id { get; set; }

	public string No { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Summary { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public decimal Price { get; set; }
}
