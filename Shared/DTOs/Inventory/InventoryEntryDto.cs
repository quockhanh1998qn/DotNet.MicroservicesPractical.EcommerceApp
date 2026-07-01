using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Inventory;

public class InventoryEntryDto
{
	public string Id { get; set; } = string.Empty;

	[Required]
	[StringLength(50, MinimumLength = 1)]
	public string ItemNo { get; set; } = string.Empty;

	[Range(0, int.MaxValue)]
	public int Quantity { get; set; }

	public string DocumentNo { get; set; } = string.Empty;

	public string DocumentType { get; set; } = string.Empty;

	[Required]
	[StringLength(255, MinimumLength = 1)]
	public string ExternalDocumentNo { get; set; } = string.Empty;
}

public class SalesItemDto
{
	[Required]
	[StringLength(50, MinimumLength = 1)]
	public string ItemNo { get; set; } = string.Empty;

	[Range(1, int.MaxValue)]
	public int Quantity { get; set; }

	[Required]
	public string ExternalDocumentNo { get; set; } = string.Empty;
}

public class PurchaseItemDto : SalesItemDto
{
}
