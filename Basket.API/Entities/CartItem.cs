namespace Basket.API.Entities;

public class CartItem
{
	public string ProductNo { get; set; } = string.Empty;
	public string ProductName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public decimal Price { get; set; }
}
