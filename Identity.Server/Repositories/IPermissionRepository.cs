using Identity.Server.Entities;

namespace Identity.Server.Repositories;

public interface IPermissionRepository
{
	Task<IReadOnlyList<Permission>> GetPermissionsByUserAsync(long userId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Permission>> GetPermissionsByRoleAsync(long roleId, CancellationToken cancellationToken = default);
	Task GrantAsync(long roleId, string function, string command, CancellationToken cancellationToken = default);
	Task RevokeAsync(long roleId, string function, string command, CancellationToken cancellationToken = default);
}
