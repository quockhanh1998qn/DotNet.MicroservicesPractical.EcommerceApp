using AutoMapper;
using Inventory.API.Entities;
using Inventory.API.Repositories;
using Shared.Common;
using Shared.DTOs.Inventory;

namespace Inventory.API.Services;

public class InventoryService : IInventoryService
{
	private readonly IInventoryRepository _repository;
	private readonly IMapper _mapper;

	public InventoryService(IInventoryRepository repository, IMapper mapper)
	{
		_repository = repository;
		_mapper = mapper;
	}

	public async Task<PagedList<InventoryEntryDto>> GetAllByItemNoAsync(string itemNo, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
	{
		var page = await _repository.GetAllByItemNoAsync(itemNo, pageNumber, pageSize, cancellationToken);
		var mapped = _mapper.Map<IReadOnlyList<InventoryEntryDto>>(page.Items);
		return new PagedList<InventoryEntryDto>(mapped, page.TotalItems, page.PageNumber, page.PageSize);
	}

	public async Task<InventoryEntryDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		var entry = await _repository.GetByIdAsync(id, cancellationToken);
		return entry is null ? null : _mapper.Map<InventoryEntryDto>(entry);
	}

	public Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default) =>
		_repository.GetStockAsync(itemNo, cancellationToken);

	public async Task<InventoryEntryDto> PurchaseAsync(PurchaseItemDto purchase, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(purchase);
		var entry = new InventoryEntry
		{
			ItemNo = purchase.ItemNo,
			Quantity = purchase.Quantity,
			ExternalDocumentNo = purchase.ExternalDocumentNo,
			DocumentType = InventoryDocumentTypes.Purchase,
			DocumentNo = Guid.NewGuid().ToString("N")
		};
		await _repository.CreateAsync(entry, cancellationToken);
		return _mapper.Map<InventoryEntryDto>(entry);
	}

	public async Task<InventoryEntryDto> SaleAsync(SalesItemDto sale, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(sale);

		var available = await _repository.GetStockAsync(sale.ItemNo, cancellationToken);
		if (available < sale.Quantity)
		{
			throw new InvalidOperationException($"Insufficient stock for item '{sale.ItemNo}'. Available: {available}, Requested: {sale.Quantity}.");
		}

		var entry = new InventoryEntry
		{
			ItemNo = sale.ItemNo,
			Quantity = -Math.Abs(sale.Quantity),
			ExternalDocumentNo = sale.ExternalDocumentNo,
			DocumentType = InventoryDocumentTypes.Sales,
			DocumentNo = Guid.NewGuid().ToString("N")
		};
		await _repository.CreateAsync(entry, cancellationToken);
		return _mapper.Map<InventoryEntryDto>(entry);
	}

	public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
		_repository.DeleteAsync(id, cancellationToken);
}
