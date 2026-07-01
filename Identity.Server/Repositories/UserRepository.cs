using Identity.Server.Entities;
using Identity.Server.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Server.Repositories;

public class UserRepository : IUserRepository
{
	private readonly IdentityContext _context;

	public UserRepository(IdentityContext context)
	{
		_context = context;
	}

	public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
		=> _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);

	public Task<User?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
		=> _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

	public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default)
		=> await _context.Users.AsNoTracking().ToListAsync(cancellationToken);
}
