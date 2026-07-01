using Inventory.API.Entities;
using MongoDB.Driver;

namespace Inventory.API.Persistence;

public interface IInventoryContext
{
	IMongoCollection<InventoryEntry> InventoryEntries { get; }
}

public class InventoryContext : IInventoryContext
{
	public IMongoCollection<InventoryEntry> InventoryEntries { get; }

	public InventoryContext(MongoDbSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		if (string.IsNullOrWhiteSpace(settings.ConnectionString))
		{
			throw new InvalidOperationException("MongoDbSettings.ConnectionString is missing.");
		}

		var client = new MongoClient(settings.ConnectionString);
		var database = client.GetDatabase(settings.DatabaseName);
		InventoryEntries = database.GetCollection<InventoryEntry>(settings.CollectionName);
	}
}

public class MongoDbSettings
{
	public string ConnectionString { get; set; } = string.Empty;
	public string DatabaseName { get; set; } = "InventoryDb";
	public string CollectionName { get; set; } = "InventoryEntries";
}
