using Contracts.Domains;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Customer.API.Entities;

public class CustomerEntity : EntityAuditBase<long>
{
	[Required]
	[Column(TypeName = "varchar(100)")]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[Column(TypeName = "varchar(100)")]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[Column(TypeName = "varchar(320)")]
	public string Email { get; set; } = string.Empty;
}
