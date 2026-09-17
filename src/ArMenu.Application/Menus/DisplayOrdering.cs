using ArMenu.Domain.Common;

namespace ArMenu.Application.Menus;

internal static class DisplayOrdering
{
    /// <summary>
    /// Applies a complete new order, e.g. from drag and drop. The requested ids must match the existing entries exactly
    /// (no omissions, duplicates or unknown ids), otherwise nothing changes.
    /// </summary>
    public static Result Apply<TEntity, TId>(
        IReadOnlyList<TEntity> entities,
        IReadOnlyList<TId> orderedIds,
        Func<TEntity, TId> idOf,
        Func<TEntity, int, Result> changeDisplayOrder)
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(orderedIds);

        var entitiesById = entities.ToDictionary(idOf);
        if (orderedIds.Count != entitiesById.Count || orderedIds.Distinct().Count() != orderedIds.Count ||
            !orderedIds.All(entitiesById.ContainsKey))
        {
            return MenuErrors.ReorderMismatch;
        }

        for (var position = 0; position < orderedIds.Count; position++)
        {
            var changed = changeDisplayOrder(entitiesById[orderedIds[position]], position);
            if (changed.IsFailure)
            {
                return changed;
            }
        }

        return Result.Success();
    }
}
