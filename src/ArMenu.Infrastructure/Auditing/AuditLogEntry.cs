using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Infrastructure.Auditing;

/// <summary>
/// One change in a business's history: who changed what, when, and how. A persistence model written by
/// <see cref="AuditTrailSaveChangesInterceptor"/>, never updated, read as a projection.
/// </summary>
/// <remarks>
/// People appear by account id only. Their names are read at display time, so erasing an account leaves "a deleted
/// account" in the history and no personal data behind.
/// </remarks>
internal sealed class AuditLogEntry : ITenantScoped
{
    /// <summary>A version 7 UUID: its order is the order of events, so it is also the paging cursor.</summary>
    public Guid Id { get; init; }

    public TenantId TenantId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>The account that made the change; <see langword="null"/> for the system (e.g. model processing).</summary>
    public UserId? ActorUserId { get; init; }

    public string Action { get; init; } = "";

    public string SubjectType { get; init; } = "";

    public Guid SubjectId { get; init; }

    /// <summary>The menu name of the subject at the time, when it has one.</summary>
    public LocalizedText? SubjectName { get; init; }

    /// <summary>A JSON array of <c>{ field, before, after }</c>.</summary>
    public string Changes { get; init; } = "[]";
}

internal static class AuditActions
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";
    public const string Reordered = "reordered";
}

internal static class AuditSubjects
{
    public const string MenuItem = "menu_item";
    public const string MenuCategory = "menu_category";

    /// <summary>The menu as a whole, e.g. when its categories are reordered.</summary>
    public const string Menu = "menu";

    /// <summary>The business's settings.</summary>
    public const string Business = "business";

    public const string Member = "member";
}
