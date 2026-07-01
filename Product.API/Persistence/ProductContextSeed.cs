namespace Product.API.Persistence;

using Product.API.Entities;
using Serilog;

public class ProductContextSeed
{
	public static async Task SeedProductAsync(ProductContext productContext, ILogger logger)
	{
		if (!productContext.Products.Any())
		{
			productContext.Products.AddRange(GetCatalogProducts());
			await productContext.SaveChangesAsync();

			logger.Information("Seeded data for Product DB associated with context {DbContextName}", nameof(ProductContext));
		}
	}

	private static IEnumerable<CatalogProduct> GetCatalogProducts()
	{
		return new List<CatalogProduct>
		{
			new()
			{
				No = "Lotus",
				Name = "Esprit",
				Summary = "Nondisplaced fracture of greater trochanter of right femur",
				Description = "Nondisplaced fracture of greater trochanter of right femur",
				Price = (decimal)177940.49
			},
			new()
			{
				No = "PRD-001",
				Name = "Wireless Bluetooth Headphones",
				Summary = "Premium over-ear headphones with active noise cancellation",
				Description = "High-fidelity wireless headphones with 30-hour battery life, foldable design, and premium memory foam ear cushions for all-day comfort.",
				Price = (decimal)129.99
			},
			new()
			{
				No = "PRD-002",
				Name = "Mechanical Gaming Keyboard",
				Summary = "RGB mechanical keyboard with Cherry MX switches",
				Description = "Full-size mechanical keyboard featuring per-key RGB lighting, programmable macro keys, and durable aluminum frame. Ideal for gaming and productivity.",
				Price = (decimal)89.50
			},
			new()
			{
				No = "PRD-003",
				Name = "Portable SSD 1TB",
				Summary = "Ultra-fast external solid state drive",
				Description = "Compact USB 3.2 Gen 2 portable SSD with read speeds up to 1050 MB/s. Shock-resistant and perfect for backups, gaming, and creative work.",
				Price = (decimal)149.00
			},
			new()
			{
				No = "PRD-004",
				Name = "Smart Watch Pro",
				Summary = "Fitness and health tracking smartwatch",
				Description = "Advanced smartwatch with heart rate monitoring, GPS, sleep tracking, and 7-day battery life. Water-resistant and compatible with iOS and Android.",
				Price = (decimal)249.99
			},
			new()
			{
				No = "PRD-005",
				Name = "Ergonomic Office Chair",
				Summary = "Adjustable lumbar support mesh chair",
				Description = "Breathable mesh back office chair with adjustable armrests, headrest, and lumbar support. Supports up to 275 lbs with a 5-year warranty.",
				Price = (decimal)329.00
			},
			new()
			{
				No = "PRD-006",
				Name = "Stainless Steel Water Bottle",
				Summary = "Insulated 32oz double-wall bottle",
				Description = "Keeps drinks cold for 24 hours or hot for 12 hours. BPA-free, leak-proof lid, and fits most cup holders.",
				Price = (decimal)34.95
			},
			new()
			{
				No = "PRD-007",
				Name = "Wireless Mouse",
				Summary = "Ergonomic wireless mouse with silent clicks",
				Description = "Lightweight ergonomic design with 2.4GHz and Bluetooth connectivity. Up to 18 months battery life and precise tracking on most surfaces.",
				Price = (decimal)45.00
			},
			new()
			{
				No = "PRD-008",
				Name = "USB-C Hub 7-in-1",
				Summary = "Multi-port adapter for laptop and tablet",
				Description = "Expand connectivity with HDMI 4K, USB 3.0, SD card reader, and 100W PD charging. Compact design for travel.",
				Price = (decimal)59.99
			},
			new()
			{
				No = "PRD-009",
				Name = "Desk Lamp with Wireless Charger",
				Summary = "LED lamp with Qi charging base",
				Description = "Adjustable LED desk lamp with 5W Qi wireless charging pad built into the base. Dimmable with multiple color temperatures.",
				Price = (decimal)79.00
			},
			new()
			{
				No = "PRD-010",
				Name = "Noise Cancelling Earbuds",
				Summary = "True wireless earbuds with ANC",
				Description = "In-ear wireless earbuds with active noise cancellation, 8-hour battery per charge, and IPX5 water resistance for workouts.",
				Price = (decimal)119.00
			}
		};
	}
}
