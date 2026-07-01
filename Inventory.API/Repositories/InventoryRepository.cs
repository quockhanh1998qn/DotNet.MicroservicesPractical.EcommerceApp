using Inventory.API.Entities;
using Inventory.API.Persistence;
using MongoDB.Driver;
using Shared.Common;

namespace Inventory.API.Repositories;

public class InventoryRepository : IInventoryRepository
{
	private readonly IInventoryContext _context;

	public InventoryRepository(IInventoryContext context)
	{
		_context = context;
	}

	public async Task<PagedList<InventoryEntry>> GetAllByItemNoAsync(string itemNo, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(itemNo);
		var filter = Builders<InventoryEntry>.Filter.Eq(x => x.ItemNo, itemNo);

		var totalItems = await _context.InventoryEntries.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

		var items = await _context.InventoryEntries
			.Find(filter)
			.SortByDescending(x => x.CreatedDate)
			.Skip((pageNumber - 1) * pageSize)
			.Limit(pageSize)
			.ToListAsync(cancellationToken);

		return new PagedList<InventoryEntry>(items, totalItems, pageNumber, pageSize);
	}

	public Task<InventoryEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
		_context.InventoryEntries.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken)!;

	public async Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(itemNo);
		var filter = Builders<InventoryEntry>.Filter.Eq(x => x.ItemNo, itemNo);
		var entries = await _context.InventoryEntries.Find(filter).ToListAsync(cancellationToken);
		return entries.Sum(e => e.Quantity);
	}

	public Task CreateAsync(InventoryEntry entry, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entry);
		return _context.InventoryEntries.InsertOneAsync(entry, cancellationToken: cancellationToken);
	}

	public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var result = await _context.InventoryEntries.DeleteOneAsync(x => x.Id == id, cancellationToken);
		return result.IsAcknowledged && result.DeletedCount > 0;
	}

	public Task DeleteByDocumentNoAsync(string documentNo, CancellationToken cancellationToken = default) =>
		_context.InventoryEntries.DeleteManyAsync(x => x.DocumentNo == documentNo, cancellationToken);
}
