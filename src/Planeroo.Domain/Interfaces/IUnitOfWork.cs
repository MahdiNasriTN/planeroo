namespace Planeroo.Domain.Interfaces;

/// <summary>
/// Unit of work pattern for transactional consistency.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
