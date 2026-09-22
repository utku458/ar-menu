using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Pricing;

namespace ArMenu.Domain.Tenants;

/// <summary>
/// A business (restaurant, café, hotel...) subscribed to the platform. The tenant is the unit of data isolation:
/// every piece of menu data belongs to exactly one tenant.
/// </summary>
public sealed class Tenant : AggregateRoot<TenantId>, IAuditable
{
    public const int NameMaxLength = 100;
    public const int MaxSupportedCultures = 10;

    private readonly List<CultureCode> _supportedCultures = [];

    private Tenant(TenantId id, string name, TenantSlug slug, CultureCode defaultCulture, Currency currency, TenantTimeZone timeZone)
        : base(id)
    {
        Name = name;
        Slug = slug;
        DefaultCulture = defaultCulture;
        Currency = currency;
        TimeZone = timeZone;
        Branding = TenantBranding.None;
        Status = TenantStatus.Active;
        _supportedCultures.Add(defaultCulture);
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private Tenant()
    {
    }
#pragma warning restore CS8618

    public string Name { get; private set; }

    /// <summary>Public handle used in menu URLs. Immutable: printed QR codes depend on it.</summary>
    public TenantSlug Slug { get; private init; }

    public TenantStatus Status { get; private set; }

    public bool IsActive => Status == TenantStatus.Active;

    /// <summary>Culture used when a translation is missing for the reader's language.</summary>
    public CultureCode DefaultCulture { get; private set; }

    /// <summary>Cultures the menu is offered in; always contains <see cref="DefaultCulture"/>.</summary>
    public IReadOnlyList<CultureCode> SupportedCultures => _supportedCultures.AsReadOnly();

    /// <summary>Currency all menu prices of the tenant are expressed in.</summary>
    public Currency Currency { get; private init; }

    /// <summary>Where the business's day begins, for anything counted per day.</summary>
    public TenantTimeZone TimeZone { get; private set; }

    /// <summary>How the guest menu looks: the business's logo and the colour it is painted with.</summary>
    public TenantBranding Branding { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>When the closed business's menu, history and files were deleted; the record itself keeps its slug taken.</summary>
    public DateTimeOffset? PurgedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Result<Tenant> Create(
        string name,
        TenantSlug slug,
        CultureCode defaultCulture,
        Currency currency,
        TenantTimeZone? timeZone = null)
    {
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(defaultCulture);
        ArgumentNullException.ThrowIfNull(currency);

        var validatedName = ValidateName(name);
        if (validatedName.IsFailure)
        {
            return validatedName.Error;
        }

        if (slug.IsReserved)
        {
            return TenantErrors.SlugReserved;
        }

        var tenant = new Tenant(TenantId.New(), validatedName.Value, slug, defaultCulture, currency, timeZone ?? TenantTimeZone.Utc);
        tenant.RaiseDomainEvent(new TenantCreatedDomainEvent(tenant.Id));

        return tenant;
    }

    /// <summary>
    /// The one workspace that is not a business: where the platform administrator's sessions live when they are not
    /// working inside a business. Every session and refresh cookie belongs to a tenant, and row-level security holds
    /// sessions to it, so the administrator needs a home tenant too — this is it.
    /// </summary>
    /// <remarks>
    /// It takes the reserved <see cref="TenantSlug.Platform"/>, which <see cref="Create"/> refuses to anyone else, so
    /// no business can collide with it. It has no menu and is never listed or served as one.
    /// </remarks>
    public static Tenant CreatePlatform(CultureCode defaultCulture, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(defaultCulture);
        ArgumentNullException.ThrowIfNull(currency);

        return new Tenant(TenantId.New(), "ArMenu", TenantSlug.Platform, defaultCulture, currency, TenantTimeZone.Utc);
    }

    public bool IsPlatform => Slug.IsPlatform;

    public Result Rename(string name)
    {
        var validatedName = ValidateName(name);
        if (validatedName.IsFailure)
        {
            return validatedName.Error;
        }

        Name = validatedName.Value;
        return Result.Success();
    }

    public Result AddSupportedCulture(CultureCode culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (_supportedCultures.Contains(culture))
        {
            return Result.Success();
        }

        if (_supportedCultures.Count >= MaxSupportedCultures)
        {
            return TenantErrors.SupportedCultureLimitReached;
        }

        _supportedCultures.Add(culture);
        return Result.Success();
    }

    public Result RemoveSupportedCulture(CultureCode culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (culture == DefaultCulture)
        {
            return TenantErrors.DefaultCultureCannotBeRemoved;
        }

        _supportedCultures.Remove(culture);
        return Result.Success();
    }

    /// <summary>
    /// Replaces the offered languages in one step. Changing them one by one could pass through states the tenant
    /// forbids (a default that is not offered, or more languages than allowed) even when the end result is valid.
    /// </summary>
    public Result SetLanguages(CultureCode defaultCulture, IEnumerable<CultureCode> supportedCultures)
    {
        ArgumentNullException.ThrowIfNull(defaultCulture);
        ArgumentNullException.ThrowIfNull(supportedCultures);

        var cultures = supportedCultures.Distinct().ToList();

        if (cultures.Count > MaxSupportedCultures)
        {
            return TenantErrors.SupportedCultureLimitReached;
        }

        if (!cultures.Contains(defaultCulture))
        {
            return TenantErrors.CultureNotSupported;
        }

        _supportedCultures.Clear();
        _supportedCultures.AddRange(cultures);
        DefaultCulture = defaultCulture;

        return Result.Success();
    }

    public Result ChangeDefaultCulture(CultureCode culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (!_supportedCultures.Contains(culture))
        {
            return TenantErrors.CultureNotSupported;
        }

        DefaultCulture = culture;
        return Result.Success();
    }

    /// <summary>
    /// Replaces the whole appearance at once. Whoever styles the menu sees the logo and the colour together, and one
    /// entry in the business's history says what the menu started looking like.
    /// </summary>
    public void ChangeBranding(TenantBranding branding)
    {
        ArgumentNullException.ThrowIfNull(branding);
        Branding = branding;
    }

    public void ChangeTimeZone(TenantTimeZone timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        TimeZone = timeZone;
    }

    public void Suspend()
    {
        if (Status is TenantStatus.Suspended or TenantStatus.Closed)
        {
            return;
        }

        Status = TenantStatus.Suspended;
        RaiseDomainEvent(new TenantSuspendedDomainEvent(Id));
    }

    public Result Reactivate()
    {
        if (Status == TenantStatus.Closed)
        {
            return TenantErrors.Closed;
        }

        if (Status == TenantStatus.Active)
        {
            return Result.Success();
        }

        Status = TenantStatus.Active;
        RaiseDomainEvent(new TenantReactivatedDomainEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Closes the business for good, when the only person who could run it deleted their account. Its menu stops being
    /// served and nobody can sign in. Closing is final: the owner who could reopen it no longer exists.
    /// </summary>
    public void Close(DateTimeOffset now)
    {
        if (Status == TenantStatus.Closed)
        {
            return;
        }

        Status = TenantStatus.Closed;
        ClosedAt = now;
        RaiseDomainEvent(new TenantClosedDomainEvent(Id));
    }

    /// <summary>Whether the data of a closed business has been kept for <paramref name="retention"/> and may go now.</summary>
    public bool IsDueForPurge(DateTimeOffset now, TimeSpan retention) =>
        Status == TenantStatus.Closed && PurgedAt is null && ClosedAt + retention <= now;

    /// <summary>Records that everything the business owned was deleted. Only a closed business is ever purged.</summary>
    public void MarkPurged(DateTimeOffset now)
    {
        if (Status != TenantStatus.Closed)
        {
            throw new InvalidOperationException("Only a closed business can be purged.");
        }

        // The logo was one of the deleted files, so the record must stop pointing at it.
        Branding = TenantBranding.None;
        PurgedAt ??= now;
    }

    private static Result<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TenantErrors.NameRequired;
        }

        var trimmed = name.Trim();
        return trimmed.Length <= NameMaxLength ? trimmed : TenantErrors.NameTooLong;
    }
}
