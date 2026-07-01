using Microsoft.AspNetCore.Identity;

namespace Identity.Server.Entities;

public class User : IdentityUser<long>
{
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset? LastModifiedDate { get; set; }
	public bool IsActive { get; set; } = true;
}

public class Role : IdentityRole<long>
{
	public string? Description { get; set; }
}
