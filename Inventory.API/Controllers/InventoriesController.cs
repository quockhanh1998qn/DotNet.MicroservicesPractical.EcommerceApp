using Inventory.API.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.DTOs.Inventory;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoriesController : ControllerBase
{
	private readonly IInventoryService _inventoryService;

	public InventoriesController(IInventoryService inventoryService)
	{
		_inventoryService = inventoryService;
	}

	[HttpGet("{itemNo}")]
	[ProducesResponseType(typeof(PagedList<InventoryEntryDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<PagedList<InventoryEntryDto>>> GetAllByItemNo(
		[FromRoute] string itemNo,
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20,
		CancellationToken cancellationToken = default)
	{
		var result = await _inventoryService.GetAllByItemNoAsync(itemNo, pageNumber, pageSize, cancellationToken);
		return Ok(result);
	}

	[HttpGet("id/{id}", Name = nameof(GetInventoryById))]
	[ProducesResponseType(typeof(InventoryEntryDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<InventoryEntryDto>> GetInventoryById([FromRoute] string id, CancellationToken cancellationToken)
	{
		var entry = await _inventoryService.GetByIdAsync(id, cancellationToken);
		return entry is null ? NotFound() : Ok(entry);
	}

	[HttpGet("stock/{itemNo}")]
	[ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
	public async Task<ActionResult<int>> GetStock([FromRoute] string itemNo, CancellationToken cancellationToken)
	{
		var stock = await _inventoryService.GetStockAsync(itemNo, cancellationToken);
		return Ok(stock);
	}

	[HttpPost("purchase")]
	[ProducesResponseType(typeof(InventoryEntryDto), StatusCodes.Status201Created)]
	public async Task<ActionResult<InventoryEntryDto>> Purchase([FromBody] PurchaseItemDto purchase, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);
		var dto = await _inventoryService.PurchaseAsync(purchase, cancellationToken);
		return CreatedAtRoute(nameof(GetInventoryById), new { id = dto.Id }, dto);
	}

	[HttpPost("sale")]
	[ProducesResponseType(typeof(InventoryEntryDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<InventoryEntryDto>> Sale([FromBody] SalesItemDto sale, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid) return ValidationProblem(ModelState);
		try
		{
			var dto = await _inventoryService.SaleAsync(sale, cancellationToken);
			return CreatedAtRoute(nameof(GetInventoryById), new { id = dto.Id }, dto);
		}
		catch (InvalidOperationException ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken cancellationToken)
	{
		var ok = await _inventoryService.DeleteAsync(id, cancellationToken);
		return ok ? NoContent() : NotFound();
	}
}
