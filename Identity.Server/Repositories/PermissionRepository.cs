using System.Data;
using Dapper;
using Identity.Server.Entities;
using Microsoft.Data.SqlClient;

namespace Identity.Server.Repositories;

/// <summary>
/// Dapper-backed permission repository. Uses stored procedures
/// (created by <c>PermissionDatabaseInitializer</c>) for read paths
/// and parameterized ad-hoc SQL for writes.
/// </summary>
public class PermissionRepository : IPermissionRepository
{
	private readonly string _connectionString;

	public PermissionRepository(IConfiguration configuration)
	{
		_connectionString = configuration.GetConnectionString("IdentitySqlConnection")
			?? throw new InvalidOperationException("ConnectionStrings:IdentitySqlConnection is missing.");
	}

	public async Task<IReadOnlyList<Permission>> GetPermissionsByUserAsync(long userId, CancellationToken cancellationToken = default)
	{
		using var connection = new SqlConnection(_connectionString);
		var rows = await connection.QueryAsync<Permission>(
			"sp_GetPermissionsByUser",
			new { UserId = userId },
			commandType: CommandType.StoredProcedure);
		return rows.AsList();
	}

	public async Task<IReadOnlyList<Permission>> GetPermissionsByRoleAsync(long roleId, CancellationToken cancellationToken = default)
	{
		using var connection = new SqlConnection(_connectionString);
		var rows = await connection.QueryAsync<Permission>(
			"sp_GetPermissionsByRole",
			new { RoleId = roleId },
			commandType: CommandType.StoredProcedure);
		return rows.AsList();
	}

	public async Task GrantAsync(long roleId, string function, string command, CancellationToken cancellationToken = default)
	{
		using var connection = new SqlConnection(_connectionString);
		await connection.ExecuteAsync(
			@"IF NOT EXISTS (SELECT 1 FROM AppPermissions WHERE RoleId=@RoleId AND [Function]=@Function AND [Command]=@Command)
			  INSERT INTO AppPermissions (RoleId, [Function], [Command]) VALUES (@RoleId, @Function, @Command)",
			new { RoleId = roleId, Function = function, Command = command });
	}

	public async Task RevokeAsync(long roleId, string function, string command, CancellationToken cancellationToken = default)
	{
		using var connection = new SqlConnection(_connectionString);
		await connection.ExecuteAsync(
			"DELETE FROM AppPermissions WHERE RoleId=@RoleId AND [Function]=@Function AND [Command]=@Command",
			new { RoleId = roleId, Function = function, Command = command });
	}
}
