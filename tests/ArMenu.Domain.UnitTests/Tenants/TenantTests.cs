using ArMenu.Domain.Media;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Tenants;

public sealed class TenantTests
{
    [Fact]
    public void Create_starts_active_offering_its_default_culture()
    {
        var tenant = Tenant.Create("  Kadıköy Burger Lab ", Make.Slug(), Make.Culture("tr"), Make.CurrencyCode()).ShouldSucceed();

        tenant.Id.Value.ShouldNotBe(Guid.Empty);
        tenant.Name.ShouldBe("Kadıköy Burger Lab");
        tenant.IsActive.ShouldBeTrue();
        tenant.SupportedCultures.ShouldBe([Make.Culture("tr")]);
        tenant.DomainEvents.ShouldHaveSingleItem().ShouldBe(new TenantCreatedDomainEvent(tenant.Id));
    }

    [Fact]
    public void Create_generates_time_ordered_identifiers()
    {
        var first = Make.NewTenant();
        var second = Make.NewTenant();

        first.Id.Value.Version.ShouldBe(7);
        second.Id.ShouldNotBe(first.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_a_name(string name)
    {
        Tenant.Create(name, Make.Slug(), Make.Culture("tr"), Make.CurrencyCode()).ShouldFailWith(TenantErrors.NameRequired);
    }

    [Fact]
    public void Create_rejects_names_longer_than_the_limit()
    {
        Tenant.Create(new string('x', Tenant.NameMaxLength + 1), Make.Slug(), Make.Culture("tr"), Make.CurrencyCode())
            .ShouldFailWith(TenantErrors.NameTooLong);
    }

    [Fact]
    public void Create_rejects_reserved_slugs()
    {
        Tenant.Create("Admin Cafe", Make.Slug("admin"), Make.Culture("tr"), Make.CurrencyCode())
            .ShouldFailWith(TenantErrors.SlugReserved);
    }

    [Fact]
    public void Suspend_and_reactivate_are_idempotent_and_raise_one_event_per_transition()
    {
        var tenant = Make.NewTenant();
        tenant.ClearDomainEvents();

        tenant.Suspend();
        tenant.Suspend();
        tenant.IsActive.ShouldBeFalse();

        tenant.Reactivate();
        tenant.Reactivate();
        tenant.IsActive.ShouldBeTrue();

        tenant.DomainEvents.ShouldBe(
        [
            new TenantSuspendedDomainEvent(tenant.Id),
            new TenantReactivatedDomainEvent(tenant.Id),
        ]);
    }

    [Fact]
    public void AddSupportedCulture_ignores_cultures_already_supported()
    {
        var tenant = Make.NewTenant();

        tenant.AddSupportedCulture(Make.Culture("en")).ShouldSucceed();
        tenant.AddSupportedCulture(Make.Culture("EN")).ShouldSucceed();

        tenant.SupportedCultures.ShouldBe([Make.Culture("tr"), Make.Culture("en")]);
    }

    [Fact]
    public void AddSupportedCulture_enforces_the_limit()
    {
        var tenant = Make.NewTenant();
        string[] cultures = ["en", "de", "ru", "ar", "fr", "es", "it", "nl", "ja"];
        foreach (var culture in cultures)
        {
            tenant.AddSupportedCulture(Make.Culture(culture)).ShouldSucceed();
        }

        tenant.AddSupportedCulture(Make.Culture("ko")).ShouldFailWith(TenantErrors.SupportedCultureLimitReached);
    }

    [Fact]
    public void RemoveSupportedCulture_never_removes_the_default_culture()
    {
        var tenant = Make.NewTenant(defaultCulture: "tr");

        tenant.RemoveSupportedCulture(Make.Culture("tr")).ShouldFailWith(TenantErrors.DefaultCultureCannotBeRemoved);
    }

    [Fact]
    public void ChangeDefaultCulture_requires_a_supported_culture()
    {
        var tenant = Make.NewTenant(defaultCulture: "tr");

        tenant.ChangeDefaultCulture(Make.Culture("en")).ShouldFailWith(TenantErrors.CultureNotSupported);

        tenant.AddSupportedCulture(Make.Culture("en")).ShouldSucceed();
        tenant.ChangeDefaultCulture(Make.Culture("en")).ShouldSucceed();
        tenant.DefaultCulture.ShouldBe(Make.Culture("en"));

        tenant.RemoveSupportedCulture(Make.Culture("tr")).ShouldSucceed();
        tenant.SupportedCultures.ShouldBe([Make.Culture("en")]);
    }

    [Fact]
    public void SetLanguages_replaces_the_offered_languages_in_one_step()
    {
        var tenant = Make.NewTenant();
        string[] current = ["en", "de", "fr", "es", "it", "nl", "ru", "ar", "fa"];
        foreach (var culture in current)
        {
            tenant.AddSupportedCulture(Make.Culture(culture)).ShouldSucceed();
        }

        // Ten other languages with a new default: valid as a whole, impossible one change at a time.
        string[] next = ["en", "de", "fr", "es", "it", "nl", "ru", "ar", "ja", "ko"];
        tenant.SetLanguages(Make.Culture("ja"), next.Select(Make.Culture)).ShouldSucceed();

        tenant.DefaultCulture.ShouldBe(Make.Culture("ja"));
        tenant.SupportedCultures.ShouldBe(next.Select(Make.Culture));
    }

    [Fact]
    public void SetLanguages_requires_the_default_among_the_offered_languages()
    {
        var tenant = Make.NewTenant();

        tenant.SetLanguages(Make.Culture("de"), [Make.Culture("tr"), Make.Culture("en")]).ShouldFailWith(TenantErrors.CultureNotSupported);

        tenant.SupportedCultures.ShouldBe([Make.Culture("tr")]);
    }

    [Fact]
    public void SetLanguages_ignores_duplicates_but_enforces_the_limit()
    {
        var tenant = Make.NewTenant();
        string[] elevenCultures = ["tr", "en", "de", "fr", "es", "it", "nl", "ru", "ar", "fa", "ja"];
        var eleven = elevenCultures.Select(Make.Culture).ToList();

        tenant.SetLanguages(Make.Culture("tr"), [Make.Culture("tr"), Make.Culture("en"), Make.Culture("en")]).ShouldSucceed();
        tenant.SupportedCultures.ShouldBe([Make.Culture("tr"), Make.Culture("en")]);

        tenant.SetLanguages(Make.Culture("tr"), eleven).ShouldFailWith(TenantErrors.SupportedCultureLimitReached);
    }

    [Fact]
    public void A_new_business_counts_its_days_in_UTC_until_it_chooses_a_time_zone()
    {
        var tenant = Make.NewTenant();
        tenant.TimeZone.ShouldBe(TenantTimeZone.Utc);

        tenant.ChangeTimeZone(TenantTimeZone.Create("Europe/Istanbul").ShouldSucceed());

        tenant.TimeZone.Id.ShouldBe("Europe/Istanbul");
    }

    [Fact]
    public void Closing_is_final()
    {
        var tenant = Make.NewTenant();
        var now = new DateTimeOffset(2026, 9, 15, 9, 0, 0, TimeSpan.Zero);
        tenant.ClearDomainEvents();

        tenant.Close(now);
        tenant.Close(now.AddDays(1));
        tenant.Suspend();

        tenant.Status.ShouldBe(TenantStatus.Closed);
        tenant.ClosedAt.ShouldBe(now);
        tenant.Reactivate().ShouldFailWith(TenantErrors.Closed);
        tenant.IsActive.ShouldBeFalse();
        tenant.DomainEvents.ShouldBe([new TenantClosedDomainEvent(tenant.Id)]);
    }

    [Fact]
    public void Only_a_business_closed_for_the_whole_retention_period_is_purged()
    {
        var tenant = Make.NewTenant();
        var closedOn = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var retention = TimeSpan.FromDays(30);

        tenant.IsDueForPurge(closedOn.AddYears(1), retention).ShouldBeFalse("an active business");
        Should.Throw<InvalidOperationException>(() => tenant.MarkPurged(closedOn));

        tenant.Close(closedOn);
        tenant.IsDueForPurge(closedOn.AddDays(29), retention).ShouldBeFalse();
        tenant.IsDueForPurge(closedOn.AddDays(30), retention).ShouldBeTrue();

        tenant.MarkPurged(closedOn.AddDays(30));
        tenant.PurgedAt.ShouldBe(closedOn.AddDays(30));
        tenant.IsDueForPurge(closedOn.AddDays(31), retention).ShouldBeFalse();
    }

    [Fact]
    public void A_business_starts_without_a_logo_or_a_colour_of_its_own() =>
        Make.NewTenant().Branding.IsEmpty.ShouldBeTrue();

    [Fact]
    public void The_appearance_is_replaced_as_a_whole()
    {
        var tenant = Make.NewTenant();
        var logo = AssetPath.Create("tenants/abc/assets/logo.webp").Value;

        tenant.ChangeBranding(new TenantBranding(logo, BrandColor.Create("#1f6f5c").Value));
        tenant.Branding.LogoPath.ShouldBe(logo);

        // Sending only a colour is how a business removes its logo, not how it keeps it.
        tenant.ChangeBranding(new TenantBranding(null, BrandColor.Create("#1f6f5c").Value));
        tenant.Branding.LogoPath.ShouldBeNull();
        tenant.Branding.AccentColor!.Value.ShouldBe("#1f6f5c");
    }

    [Fact]
    public void Purging_stops_the_record_pointing_at_the_logo_file_it_deleted()
    {
        var tenant = Make.NewTenant();
        var closedOn = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        tenant.ChangeBranding(new TenantBranding(AssetPath.Create("tenants/abc/assets/logo.webp").Value, null));

        tenant.Close(closedOn);
        tenant.MarkPurged(closedOn.AddDays(30));

        tenant.Branding.LogoPath.ShouldBeNull();
    }
}
