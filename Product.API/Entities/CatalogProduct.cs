using Contracts.Domains;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Product.API.Entities;

public class CatalogProduct : EntityAuditBase<long>
{
	[Required]
	[Column(TypeName = "varchar(150)")]
	public string No { get; set; } = string.Empty;

	[Required]
	[Column(TypeName = "varchar(250)")]
	public string Name { get; set; } = string.Empty;

	[Required]
	[Column(TypeName = "varchar(255)")]
	public string Summary { get; set; } = string.Empty;

	[Column(TypeName = "text")]
	public string Description { get; set; } = string.Empty;

	[Column(TypeName = "decimal(12,2)")]
	public decimal Price { get; set; }
}
