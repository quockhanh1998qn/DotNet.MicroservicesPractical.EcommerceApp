using Identity.Server.Entities;

namespace Identity.Server.Repositories;

public interface IUserRepository
{
	Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
	Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);
}
