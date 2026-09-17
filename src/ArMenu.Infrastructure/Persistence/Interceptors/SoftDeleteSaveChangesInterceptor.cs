using ArMenu.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ArMenu.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Turns deletions of <see cref="ISoftDeletable"/> entities into updates, so no code path can hard-delete them by accident.
/// </summary>
internal sealed class SoftDeleteSaveChangesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ConvertDeletions(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ConvertDeletions(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ConvertDeletions(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var deletedEntries = context.ChangeTracker
            .Entries<ISoftDeletable>()
            .Where(entry => entry.State == EntityState.Deleted)
            .ToList();

        if (deletedEntries.Count == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        foreach (var entry in deletedEntries)
        {
            // Unchanged first, so that only the soft-delete columns (and the audit timestamp) are updated.
            entry.State = EntityState.Unchanged;
            entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
            entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = now;
        }
    }
}
