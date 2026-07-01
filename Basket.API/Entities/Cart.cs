namespace Basket.API.Entities;

public class Cart
{
	public string Username { get; set; } = string.Empty;
	public List<CartItem> Items { get; set; } = new();

	public Cart()
	{
	}

	public Cart(string username)
	{
		Username = username;
	}

	public decimal TotalPrice => Items?.Sum(i => i.Price * i.Quantity) ?? 0m;
}
