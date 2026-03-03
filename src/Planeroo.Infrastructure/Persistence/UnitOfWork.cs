using Planeroo.Domain.Interfaces;

namespace Planeroo.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly PlanerooDbContext _context;

    public UnitOfWork(PlanerooDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
