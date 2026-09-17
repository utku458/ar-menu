using ArMenu.Domain.ArModels;
using ArMenu.Domain.Menus;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class ArModelProcessingRepository(ArMenuDbContext dbContext) : IArModelProcessingRepository
{
    public Task<ArModelProcessing?> GetByIdAsync(ArModelProcessingId id, CancellationToken cancellationToken = default) =>
        dbContext.ArModelProcessings.SingleOrDefaultAsync(processing => processing.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ArModelProcessing>> ListUnfinishedForItemAsync(MenuItemId menuItemId, CancellationToken cancellationToken = default) =>
        await dbContext.ArModelProcessings
            .Where(processing => processing.MenuItemId == menuItemId &&
                (processing.Status == ArModelProcessingStatus.Queued || processing.Status == ArModelProcessingStatus.Processing))
            .ToListAsync(cancellationToken);

    public void Add(ArModelProcessing processing) => dbContext.ArModelProcessings.Add(processing);
}
