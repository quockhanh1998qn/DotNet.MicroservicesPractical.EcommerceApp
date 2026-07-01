using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Customer;

public class UpdateCustomerDto
{
	[Required]
	[StringLength(100, MinimumLength = 2)]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	[StringLength(100, MinimumLength = 2)]
	public string LastName { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	[StringLength(320)]
	public string Email { get; set; } = string.Empty;
}
