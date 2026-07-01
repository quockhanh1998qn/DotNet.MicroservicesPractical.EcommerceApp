using AutoMapper;
using Inventory.API;
using Inventory.API.Entities;
using Inventory.API.Repositories;
using Inventory.API.Services;
using Shared.Common;
using Shared.DTOs.Inventory;

namespace Inventory.API.UnitTests;

public class InventoryServiceTests
{
	private static readonly IMapper Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

	[Fact]
	public async Task Purchase_AddsPositiveQuantityEntry()
	{
		var repo = new InMemoryRepo();
		var service = new InventoryService(repo, Mapper);

		var dto = await service.PurchaseAsync(new PurchaseItemDto
		{
			ItemNo = "P-001",
			Quantity = 5,
			ExternalDocumentNo = "PO-1"
		}, CancellationToken.None);

		Assert.Equal("Purchase", dto.DocumentType);
		Assert.Equal(5, dto.Quantity);
		Assert.Single(repo.Created);
	}

	[Fact]
	public async Task Sale_RegistersNegativeQuantity_WhenStockSufficient()
	{
		var repo = new InMemoryRepo();
		repo.Created.Add(new InventoryEntry { ItemNo = "P-001", Quantity = 10 });
		var service = new InventoryService(repo, Mapper);

		var dto = await service.SaleAsync(new SalesItemDto { ItemNo = "P-001", Quantity = 3, ExternalDocumentNo = "SO-1" }, CancellationToken.None);

		Assert.Equal(-3, dto.Quantity);
		Assert.Equal("Sales", dto.DocumentType);
	}

	[Fact]
	public async Task Sale_Throws_WhenStockInsufficient()
	{
		var repo = new InMemoryRepo();
		repo.Created.Add(new InventoryEntry { ItemNo = "P-001", Quantity = 2 });
		var service = new InventoryService(repo, Mapper);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			service.SaleAsync(new SalesItemDto { ItemNo = "P-001", Quantity = 5, ExternalDocumentNo = "SO-2" }, CancellationToken.None));
	}

	[Fact]
	public async Task GetStock_SumsAllEntries()
	{
		var repo = new InMemoryRepo();
		repo.Created.AddRange(new[]
		{
			new InventoryEntry { ItemNo = "P-001", Quantity = 10 },
			new InventoryEntry { ItemNo = "P-001", Quantity = -3 },
			new InventoryEntry { ItemNo = "P-002", Quantity = 99 }
		});
		var service = new InventoryService(repo, Mapper);

		var stock = await service.GetStockAsync("P-001");

		Assert.Equal(7, stock);
	}

	[Fact]
	public void PagedList_ComputesPagesCorrectly()
	{
		var page = new PagedList<int>(new[] { 1, 2, 3 }, totalItems: 25, pageNumber: 2, pageSize: 10);

		Assert.Equal(3, page.TotalPages);
		Assert.True(page.HasPrevious);
		Assert.True(page.HasNext);
	}

	private sealed class InMemoryRepo : IInventoryRepository
	{
		public List<InventoryEntry> Created { get; } = new();

		public Task<PagedList<InventoryEntry>> GetAllByItemNoAsync(string itemNo, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
		{
			var items = Created.Where(x => x.ItemNo == itemNo).ToList();
			return Task.FromResult(new PagedList<InventoryEntry>(items, items.Count, pageNumber, pageSize));
		}

		public Task<InventoryEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
			Task.FromResult<InventoryEntry?>(Created.FirstOrDefault(x => x.Id == id));

		public Task<int> GetStockAsync(string itemNo, CancellationToken cancellationToken = default) =>
			Task.FromResult(Created.Where(x => x.ItemNo == itemNo).Sum(x => x.Quantity));

		public Task CreateAsync(InventoryEntry entry, CancellationToken cancellationToken = default)
		{
			Created.Add(entry);
			return Task.CompletedTask;
		}

		public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
		{
			var removed = Created.RemoveAll(x => x.Id == id) > 0;
			return Task.FromResult(removed);
		}

		public Task DeleteByDocumentNoAsync(string documentNo, CancellationToken cancellationToken = default)
		{
			Created.RemoveAll(x => x.DocumentNo == documentNo);
			return Task.CompletedTask;
		}
	}
}
