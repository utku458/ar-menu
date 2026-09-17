using ArMenu.Domain.Menus;

namespace ArMenu.Domain.ArModels;

public interface IArModelProcessingRepository
{
    Task<ArModelProcessing?> GetByIdAsync(ArModelProcessingId id, CancellationToken cancellationToken = default);

    /// <summary>Processings of the item that are queued or running.</summary>
    Task<IReadOnlyList<ArModelProcessing>> ListUnfinishedForItemAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default);

    void Add(ArModelProcessing processing);
}
