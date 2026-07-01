using Inventory.API.Entities;
using Shared.Common;

namespace Inventory.API.Repositories;

public interface IInventoryRepository
{
	Task<PagedList<InventoryEntry>> GetAllByItemNoAsync(string itemNo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
	Task<InventoryEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
	Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default);
	Task CreateAsync(InventoryEntry entry, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
	Task DeleteByDocumentNoAsync(string documentNo, CancellationToken cancellationToken = default);
}
