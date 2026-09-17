using System.Text.Json;
using System.Text.Json.Nodes;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ArMenu.Infrastructure.Auditing;

/// <summary>
/// Writes a business's history from the changes being saved, in the same transaction: menu items and categories, the
/// business's settings and its team. Recording at the persistence boundary means no command handler can forget it, and
/// an entry exists exactly when the change it describes was committed.
/// </summary>
/// <remarks>
/// Only fields people care about are recorded (a price, not an audit timestamp). Reordering touches many rows for one
/// intention, so it becomes one entry per list. Runs before the other interceptors, while deletions are still deletions.
/// </remarks>
internal sealed class AuditTrailSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly List<AuditLogEntry> _pending = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Record(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Record(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        _pending.Clear();
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        _pending.Clear();
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => Discard(eventData?.Context);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData?.Context);
        return Task.CompletedTask;
    }

    // A retried save must not record the same change twice.
    private void Discard(DbContext? context)
    {
        foreach (var entry in _pending)
        {
            if (context is not null)
            {
                context.Entry(entry).State = EntityState.Detached;
            }
        }

        _pending.Clear();
    }

    private void Record(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actor = currentUser.UserId;
        var reorderedLists = new Dictionary<(TenantId Tenant, string Type, Guid Id), LocalizedText?>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var recorded = entry.Entity switch
            {
                MenuItem item => Describe(entry, AuditSubjects.MenuItem, item.TenantId, item.Id.Value, item.Name, MenuItemFields),
                MenuCategory category => Describe(entry, AuditSubjects.MenuCategory, category.TenantId, category.Id.Value, category.Name, MenuCategoryFields),
                Tenant tenant when entry.State == EntityState.Modified => Describe(entry, AuditSubjects.Business, tenant.Id, tenant.Id.Value, null, BusinessFields),

                // The owner's membership is added when the business is created; that is not a change to its team.
                TenantMembership { IsOwner: true } when entry.State == EntityState.Added => null,
                TenantMembership membership => Describe(entry, AuditSubjects.Member, membership.TenantId, membership.UserId.Value, null, MemberFields),
                _ => null,
            };

            if (recorded is { Changes.Count: 0 } && entry.State == EntityState.Modified)
            {
                // Nothing recorded changed; a new position alone means the list it belongs to was reordered.
                if (entry.Entity is MenuItem movedItem && IsModified(entry, nameof(MenuItem.DisplayOrder)))
                {
                    reorderedLists.TryAdd((movedItem.TenantId, AuditSubjects.MenuCategory, movedItem.CategoryId.Value), CategoryName(context, movedItem.CategoryId));
                }
                else if (entry.Entity is MenuCategory movedCategory && IsModified(entry, nameof(MenuCategory.DisplayOrder)))
                {
                    reorderedLists.TryAdd((movedCategory.TenantId, AuditSubjects.Menu, movedCategory.TenantId.Value), null);
                }

                continue;
            }

            if (recorded is not null)
            {
                // Someone who joins a team by accepting an invitation acts for themselves, before they have a session.
                var entryActor = actor ?? (entry.Entity is TenantMembership joined && entry.State == EntityState.Added ? joined.UserId : null);
                Add(context, recorded, entryActor, now);
            }
        }

        foreach (var ((tenantId, type, id), name) in reorderedLists)
        {
            Add(context, new RecordedChange(tenantId, AuditActions.Reordered, type, id, name, []), actor, now);
        }
    }

    private void Add(DbContext context, RecordedChange change, UserId? actor, DateTimeOffset now)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.CreateVersion7(now),
            TenantId = change.TenantId,
            OccurredAt = now,
            ActorUserId = actor,
            Action = change.Action,
            SubjectType = change.SubjectType,
            SubjectId = change.SubjectId,
            SubjectName = change.SubjectName,
            Changes = JsonSerializer.Serialize(change.Changes, AuditJson.Options),
        };

        context.Add(entry);
        _pending.Add(entry);
    }

    private static RecordedChange Describe(
        EntityEntry entry,
        string subjectType,
        TenantId tenantId,
        Guid subjectId,
        LocalizedText? subjectName,
        IReadOnlyList<AuditedField> fields)
    {
        var action = entry.State switch
        {
            EntityState.Added => AuditActions.Created,
            EntityState.Deleted => AuditActions.Deleted,
            _ => AuditActions.Updated,
        };

        List<AuditChange> changes = [];
        if (entry.State == EntityState.Modified)
        {
            foreach (var field in fields)
            {
                var (before, after) = field.Read(entry);
                if (!Equals(before, after))
                {
                    changes.Add(new AuditChange(field.Name, field.ToJson(before), field.ToJson(after)));
                }
            }
        }

        return new RecordedChange(tenantId, action, subjectType, subjectId, subjectName, changes);
    }

    private static bool IsModified(EntityEntry entry, string property) =>
        !Equals(entry.Property(property).OriginalValue, entry.Property(property).CurrentValue);

    private static LocalizedText? CategoryName(DbContext context, MenuCategoryId categoryId) =>
        context.ChangeTracker.Entries<MenuCategory>().FirstOrDefault(category => category.Entity.Id == categoryId)?.Entity.Name;

    private static (object? Before, object? After) Scalar(EntityEntry entry, string property)
    {
        var values = entry.Property(property);
        return (values.OriginalValue, values.CurrentValue);
    }

    private static (object? Before, object? After) Nested(EntityEntry entry, string complexProperty, string property)
    {
        var values = entry.ComplexProperty(complexProperty).Property(property);
        return (values.OriginalValue, values.CurrentValue);
    }

    private static JsonObject? Localized(object? value) =>
        value is LocalizedText text ? new JsonObject(text.Translations.Select(pair => KeyValuePair.Create(pair.Key, (JsonNode?)pair.Value))) : null;

    private static JsonNode? Plain(object? value) => value switch
    {
        null => null,
        bool flag => JsonValue.Create(flag),
        decimal number => JsonValue.Create(number),
        MenuCategoryId id => JsonValue.Create(id.Value),
        IEnumerable<CultureCode> cultures => new JsonArray([.. cultures.Select(culture => (JsonNode?)JsonValue.Create(culture.Value))]),
        CodeList { Codes: null } => null,
        CodeList list => new JsonArray([.. list.Codes!.Select(code => (JsonNode?)JsonValue.Create(code))]),
        _ => JsonValue.Create(value.ToString()),
    };

    // Whether a file is there, not where it is stored: keys mean nothing to people. Replacing one shows as true -> true.
    private static JsonValue Presence(object? value) => JsonValue.Create(value is not null);

    private static readonly AuditedField[] MenuItemFields =
    [
        new("name", entry => Scalar(entry, nameof(MenuItem.Name)), Localized),
        new("description", entry => Scalar(entry, nameof(MenuItem.Description)), Localized),
        new("price", entry => Nested(entry, nameof(MenuItem.Price), nameof(Money.Amount)), Plain),
        new("category", entry => Scalar(entry, nameof(MenuItem.CategoryId)), Plain),
        new("photo", entry => Scalar(entry, nameof(MenuItem.ImagePath)), Presence),
        new("model", entry => Nested(entry, nameof(MenuItem.ArModel), nameof(ArModel.GlbPath)), Presence),
        new("visible", entry => Scalar(entry, nameof(MenuItem.IsVisible)), Plain),
        new("available", entry => Scalar(entry, nameof(MenuItem.IsAvailable)), Plain),
        new("allergens", entry => Codes<Allergen>(entry, nameof(MenuItem.Allergens), DietaryInformation.CodeOf), Plain),
        new("dietaryLabels", entry => Codes<DietaryLabel>(entry, nameof(MenuItem.DietaryLabels), DietaryInformation.CodeOf), Plain),
    ];

    private static readonly AuditedField[] MenuCategoryFields =
    [
        new("name", entry => Scalar(entry, nameof(MenuCategory.Name)), Localized),
        new("description", entry => Scalar(entry, nameof(MenuCategory.Description)), Localized),
        new("visible", entry => Scalar(entry, nameof(MenuCategory.IsVisible)), Plain),
    ];

    private static readonly AuditedField[] BusinessFields =
    [
        new("name", entry => Scalar(entry, nameof(Tenant.Name)), Plain),
        new("defaultLanguage", entry => Scalar(entry, nameof(Tenant.DefaultCulture)), Plain),
        new("languages", entry => Languages(entry), Plain),
        new("timeZone", entry => Scalar(entry, nameof(Tenant.TimeZone)), Plain),
        new("logo", entry => Nested(entry, nameof(Tenant.Branding), nameof(TenantBranding.LogoPath)), Presence),
        new("accentColor", entry => Nested(entry, nameof(Tenant.Branding), nameof(TenantBranding.AccentColor)), Plain),
    ];

    private static readonly AuditedField[] MemberFields =
    [
        new("role", entry => Scalar(entry, nameof(TenantMembership.Role)), Plain),
    ];

    // Collections compare by reference; the snapshot and the current list are compared by their contents instead.
    private static (object? Before, object? After) Languages(EntityEntry entry)
    {
        var (before, after) = Scalar(entry, nameof(Tenant.SupportedCultures));
        var beforeCodes = (before as IEnumerable<CultureCode>)?.ToList() ?? [];
        var afterCodes = (after as IEnumerable<CultureCode>)?.ToList() ?? [];
        return beforeCodes.SequenceEqual(afterCodes) ? (null, null) : (beforeCodes, afterCodes);
    }

    // Allergens and labels as their codes, compared by content. "Not declared" (null) and "none" ([]) stay different.
    private static (object? Before, object? After) Codes<TValue>(EntityEntry entry, string property, Func<TValue, string> code)
    {
        var (before, after) = Scalar(entry, property);
        var beforeCodes = (before as IEnumerable<TValue>)?.Select(code).ToList();
        var afterCodes = (after as IEnumerable<TValue>)?.Select(code).ToList();
        var same = beforeCodes is null ? afterCodes is null : afterCodes is not null && beforeCodes.SequenceEqual(afterCodes);
        return same ? (null, null) : (new CodeList(beforeCodes), new CodeList(afterCodes));
    }

    private sealed record CodeList(IReadOnlyList<string>? Codes);

    private sealed record AuditedField(string Name, Func<EntityEntry, (object? Before, object? After)> Read, Func<object?, JsonNode?> ToJson);

    private sealed record RecordedChange(
        TenantId TenantId,
        string Action,
        string SubjectType,
        Guid SubjectId,
        LocalizedText? SubjectName,
        IReadOnlyList<AuditChange> Changes);
}

/// <summary>One recorded field of a change, as stored and as the API returns it.</summary>
internal sealed record AuditChange(string Field, JsonNode? Before, JsonNode? After);

internal static class AuditJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
