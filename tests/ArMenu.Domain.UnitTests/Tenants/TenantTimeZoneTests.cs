using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Tenants;

public sealed class TenantTimeZoneTests
{
    [Theory]
    [InlineData("Europe/Istanbul")]
    [InlineData("America/Argentina/Buenos_Aires")]
    [InlineData("UTC")]
    public void IANA_names_are_accepted(string id) =>
        TenantTimeZone.Create($" {id} ").ShouldSucceed().Id.ShouldBe(id);

    [Fact]
    public void The_database_spelling_is_kept_whatever_the_case_of_the_input() =>
        TenantTimeZone.Create("europe/istanbul").ShouldSucceed().ShouldBe(TenantTimeZone.Create("Europe/Istanbul").ShouldSucceed());

    [Theory]
    [InlineData("Turkey Standard Time")]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("+03:00")]
    public void Other_names_are_refused(string id) =>
        TenantTimeZone.Create(id).ShouldFailWith(TenantErrors.TimeZoneInvalid);

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void A_time_zone_is_required(string? id) =>
        TenantTimeZone.Create(id).ShouldFailWith(TenantErrors.TimeZoneRequired);

    [Fact]
    public void A_day_begins_at_local_midnight()
    {
        var istanbul = TenantTimeZone.Create("Europe/Istanbul").ShouldSucceed();
        var lateEvening = new DateTimeOffset(2026, 9, 15, 20, 59, 0, TimeSpan.Zero);

        istanbul.DateAt(lateEvening).ShouldBe(new DateOnly(2026, 9, 15));
        istanbul.DateAt(lateEvening.AddMinutes(1)).ShouldBe(new DateOnly(2026, 9, 16));
        TenantTimeZone.Utc.DateAt(lateEvening.AddMinutes(1)).ShouldBe(new DateOnly(2026, 9, 15));
    }
}
