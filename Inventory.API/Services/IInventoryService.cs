using Inventory.API.Entities;
using Shared.Common;
using Shared.DTOs.Inventory;

namespace Inventory.API.Services;

public interface IInventoryService
{
	Task<PagedList<InventoryEntryDto>> GetAllByItemNoAsync(string itemNo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
	Task<InventoryEntryDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
	Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default);
	Task<InventoryEntryDto> PurchaseAsync(PurchaseItemDto purchase, CancellationToken cancellationToken = default);
	Task<InventoryEntryDto> SaleAsync(SalesItemDto sale, CancellationToken cancellationToken = default);
	Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
