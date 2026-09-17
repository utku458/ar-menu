namespace ArMenu.Domain.Common;

/// <summary>Commits every change made through repositories in the current scope as one atomic transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
