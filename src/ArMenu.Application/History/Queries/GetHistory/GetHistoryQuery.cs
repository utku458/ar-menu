using System.Text.Json.Nodes;
using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.History.Queries.GetHistory;

/// <summary>
/// The business's history, newest first, a page at a time. <c>Before</c> is the previous page's
/// <see cref="HistoryPage.NextCursor"/>, <see langword="null"/> for the first page.
/// </summary>
public sealed record GetHistoryQuery(Guid? Before, int Limit) : IQuery<Result<HistoryPage>>
{
    public const int MaxLimit = 100;
}

public sealed record HistoryPage(IReadOnlyList<HistoryEntryResponse> Entries, Guid? NextCursor);

/// <summary>
/// One change. <c>Action</c> is <c>created</c>, <c>updated</c>, <c>deleted</c> or <c>reordered</c>; <c>Actor</c> is
/// <see langword="null"/> when the system made the change (e.g. 3D model processing).
/// </summary>
public sealed record HistoryEntryResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    HistoryPerson? Actor,
    string Action,
    HistorySubject Subject,
    IReadOnlyList<HistoryChange> Changes);

/// <summary>An account; <c>FullName</c> is <see langword="null"/> once the person deleted it.</summary>
public sealed record HistoryPerson(Guid Id, string? FullName);

/// <summary>
/// What changed. <c>Type</c> is <c>menu_item</c>, <c>menu_category</c>, <c>menu</c>, <c>business</c> or <c>member</c>.
/// Menu items and categories carry their name in every language at the time of the change; members carry the person.
/// </summary>
public sealed record HistorySubject(string Type, Guid Id, IReadOnlyDictionary<string, string>? Name, HistoryPerson? Person);

/// <summary>
/// A recorded field. Menu items: <c>name</c>, <c>description</c> (translations), <c>price</c> (amount),
/// <c>category</c> (id), <c>photo</c>, <c>model</c> (present or not), <c>visible</c>, <c>available</c>. Categories:
/// <c>name</c>, <c>description</c>, <c>visible</c>. Business: <c>name</c>, <c>defaultLanguage</c>, <c>languages</c>,
/// <c>timeZone</c>. Members: <c>role</c>.
/// </summary>
public sealed record HistoryChange(string Field, JsonNode? Before, JsonNode? After);
