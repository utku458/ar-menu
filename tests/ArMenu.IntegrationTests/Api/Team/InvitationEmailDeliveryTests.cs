using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Team;

/// <summary>The real SMTP sender against a real SMTP server: the invitation that arrives is the one that works.</summary>
public sealed partial class InvitationEmailDeliveryTests(PostgresDatabaseFixture database, MailpitFixture mailpit)
    : IClassFixture<MailpitFixture>, IAsyncDisposable
{
    private readonly List<ArMenuApiFactory> _factories = [];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_invitation_that_arrives_opens_the_team()
    {
        var factory = Factory(mailpit.Host, mailpit.Port);
        var restaurant = await database.Services.SeedTenantAsync(Ct, tenant => tenant.Rename("Kadıköy <Burger> & Co"));
        using var owner = await factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var address = $"cook-{Guid.NewGuid():N}@armenu.test";

        using var invited = await owner.PostAsJsonAsync("/api/v1/manage/team/invitations", new { email = address, role = "Staff", language = "tr" }, Ct);

        (await invited.ReadJsonAsync()).GetProperty("emailSent").GetBoolean().ShouldBeTrue();
        var message = await mailpit.LatestMessageToAsync(address, Ct);
        message.From.Address.ShouldBe("no-reply@armenu.test");
        message.Subject.ShouldContain("Kadıköy <Burger> & Co");
        message.Html.ShouldContain("Kadıköy &lt;Burger&gt; &amp; Co");
        message.Html.ShouldNotContain("<Burger>");

        var link = new Uri(Link().Match(message.Text).Value);
        using var accepted = await factory.CreateBrowserClient().PostAsJsonAsync(
            $"/api/v1/tenants/{restaurant.Tenant.Slug}/invitations/accept",
            new { token = link.Fragment.TrimStart('#'), fullName = "Mehmet Aşçı", password = "a-long-enough-new-password" },
            Ct);
        accepted.StatusCode.ShouldBe(HttpStatusCode.OK, await accepted.Content.ReadAsStringAsync(Ct));

        // The HTML button carries the same link.
        message.Html.ShouldContain($"href=\"{link.AbsoluteUri}\"");
    }

    [Fact]
    public async Task An_unreachable_mail_server_is_reported_not_thrown()
    {
        var factory = Factory("127.0.0.1", port: 9);
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        using var invited = await owner.PostAsJsonAsync("/api/v1/manage/team/invitations", new { email = "nobody@armenu.test", role = "Staff" }, Ct);

        invited.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await invited.ReadJsonAsync()).GetProperty("emailSent").GetBoolean().ShouldBeFalse();
    }

    private ArMenuApiFactory Factory(string host, int port)
    {
        var factory = new ArMenuApiFactory(database, settings: new Dictionary<string, string?>
        {
            ["Email:Host"] = host,
            ["Email:Port"] = port.ToString(CultureInfo.InvariantCulture),
            ["Email:Security"] = "None",
            ["Email:Timeout"] = "00:00:05",
        });
        _factories.Add(factory);
        return factory;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }
    }

    [GeneratedRegex(@"https://\S+/join#\S+")]
    private static partial Regex Link();
}
