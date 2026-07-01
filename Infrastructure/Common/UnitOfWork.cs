using Contracts.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common;

public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
{
	private readonly TContext _context;

	public UnitOfWork(TContext context)
	{
		_context = context;
	}

	public void Dispose() => _context.Dispose();

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);

	public Task<int> CommitAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(cancellationToken);
}
