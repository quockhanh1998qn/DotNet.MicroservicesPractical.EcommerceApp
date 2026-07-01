using Microsoft.Data.SqlClient;

namespace Identity.Server.Persistence;

/// <summary>
/// Creates stored procedures used by <c>PermissionRepository</c>.
/// Idempotent: safe to run on every startup.
/// </summary>
public static class PermissionDatabaseInitializer
{
	private const string SpGetPermissionsByUser = @"
CREATE OR ALTER PROCEDURE sp_GetPermissionsByUser
	@UserId BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT p.Id, p.RoleId, p.[Function], p.[Command]
	FROM AppPermissions p
	INNER JOIN AspNetUserRoles ur ON ur.RoleId = p.RoleId
	WHERE ur.UserId = @UserId;
END";

	private const string SpGetPermissionsByRole = @"
CREATE OR ALTER PROCEDURE sp_GetPermissionsByRole
	@RoleId BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT Id, RoleId, [Function], [Command]
	FROM AppPermissions
	WHERE RoleId = @RoleId;
END";

	public static async Task EnsureCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
	{
		await using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync(cancellationToken);

		foreach (var script in new[] { SpGetPermissionsByUser, SpGetPermissionsByRole })
		{
			await using var cmd = connection.CreateCommand();
			cmd.CommandText = script;
			await cmd.ExecuteNonQueryAsync(cancellationToken);
		}
	}
}
