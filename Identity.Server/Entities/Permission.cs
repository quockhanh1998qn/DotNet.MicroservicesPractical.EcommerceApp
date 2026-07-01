namespace Identity.Server.Entities;

/// <summary>
/// Fine-grained permission entity used by Dapper repositories and stored procedures.
/// Permissions are role-scoped: a role can grant access to a set of (function, command) pairs.
/// </summary>
public class Permission
{
	public long Id { get; set; }
	public long RoleId { get; set; }
	public string Function { get; set; } = string.Empty;
	public string Command { get; set; } = string.Empty;
}
