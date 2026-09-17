using ArMenu.Application.Abstractions.Persistence;
using ArMenu.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ArMenu.Infrastructure.Persistence;

/// <summary>Commits the scope's changes and translates provider exceptions into application-level ones.</summary>
internal sealed class UnitOfWork(ArMenuDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The data was modified by another operation.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new UniqueConstraintViolationException("The data conflicts with existing data.", exception);
        }
    }
}
