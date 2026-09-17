using System.Text.Json;
using ArMenu.Application.History.Queries.GetHistory;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure.Auditing;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.History;

public sealed class GetHistoryQueryHandler(ArMenuDbContext dbContext) : IQueryHandler<GetHistoryQuery, Result<HistoryPage>>
{
    public async ValueTask<Result<HistoryPage>> Handle(GetHistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var entries = dbContext.Set<AuditLogEntry>().AsNoTracking();
        if (query.Before is { } before)
        {
            entries = entries.Where(entry => entry.Id < before);
        }

        // One more than asked tells whether there is a next page.
        var page = await entries
            .OrderByDescending(entry => entry.Id)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > query.Limit;
        page = [.. page.Take(query.Limit)];

        // Names are read now, not stored with the entry: an erased account shows as deleted everywhere at once.
        var userIds = page
            .SelectMany(entry => new[] { entry.ActorUserId?.Value, entry.SubjectType == AuditSubjects.Member ? entry.SubjectId : null })
            .OfType<Guid>()
            .Distinct()
            .Select(UserId.From)
            .ToList();
        var people = await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new { user.Id, user.FullName, user.ErasedAt })
            .ToDictionaryAsync(user => user.Id.Value, user => user.ErasedAt == null ? user.FullName : null, cancellationToken);

        HistoryPerson? Person(Guid? id) => id is { } userId ? new HistoryPerson(userId, people.GetValueOrDefault(userId)) : null;

        return new HistoryPage(
            [.. page.Select(entry => new HistoryEntryResponse(
                entry.Id,
                entry.OccurredAt,
                Person(entry.ActorUserId?.Value),
                entry.Action,
                new HistorySubject(
                    entry.SubjectType,
                    entry.SubjectId,
                    entry.SubjectName?.Translations,
                    entry.SubjectType == AuditSubjects.Member ? Person(entry.SubjectId) : null),
                [.. (JsonSerializer.Deserialize<List<AuditChange>>(entry.Changes, AuditJson.Options) ?? [])
                    .Select(change => new HistoryChange(change.Field, change.Before, change.After))]))],
            hasMore ? page[^1].Id : null);
    }
}
