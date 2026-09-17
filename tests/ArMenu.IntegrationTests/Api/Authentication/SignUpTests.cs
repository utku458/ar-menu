using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Api.Endpoints.Tenants;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Authentication;

public sealed class SignUpTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sign_up_creates_the_business_and_signs_the_owner_in()
    {
        using var client = _factory.CreateBrowserClient();
        var slug = UniqueSlug();

        using var response = await client.PostAsJsonAsync("/api/v1/tenants", SignUpBody(slug), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.OriginalString.ShouldBe($"/api/v1/menus/{slug}");
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        response.RefreshCookieHeader().ShouldNotBeNull();

        var signUp = await response.Content.ReadFromJsonAsync<SignUpEndpoints.SignUpResponse>(Ct);
        var me = await client.Authenticate(signUp!.AccessToken)
            .GetFromJsonAsync<AuthenticationEndpoints.CurrentUserResponse>("/api/v1/me", Ct);

        me!.Role.ShouldBe("Owner");
        me.Tenant.Slug.ShouldBe(slug);
        me.Tenant.Id.ShouldBe(signUp.TenantId);
    }

    [Fact]
    public async Task A_slug_probed_before_sign_up_is_not_stuck_as_not_found()
    {
        using var client = _factory.CreateBrowserClient();
        var slug = UniqueSlug();

        using (var probe = await client.GetAsync($"/api/v1/menus/{slug}", Ct))
        {
            probe.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        using (var signUp = await client.PostAsJsonAsync("/api/v1/tenants", SignUpBody(slug), Ct))
        {
            signUp.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        using var menu = await client.GetAsync($"/api/v1/menus/{slug}", Ct);
        menu.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Slugs_and_email_addresses_can_only_be_claimed_once()
    {
        using var client = _factory.CreateBrowserClient();
        var slug = UniqueSlug();
        var original = SignUpBody(slug);

        using (var first = await client.PostAsJsonAsync("/api/v1/tenants", original, Ct))
        {
            first.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        using var sameSlug = await client.PostAsJsonAsync("/api/v1/tenants", SignUpBody(slug), Ct);
        using var sameEmail = await client.PostAsJsonAsync("/api/v1/tenants", SignUpBody(UniqueSlug()) with { OwnerEmail = original.OwnerEmail }, Ct);

        sameSlug.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await sameSlug.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("tenant.slug_taken");
        sameEmail.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await sameEmail.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("user.email_taken");
    }

    [Fact]
    public async Task Invalid_sign_ups_report_every_problem_at_once()
    {
        using var client = _factory.CreateBrowserClient();
        var body = SignUpBody("admin") with { OwnerEmail = "nope", Password = "short" };

        using var response = await client.PostAsJsonAsync("/api/v1/tenants", body, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ReadJsonAsync();
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name)
            .ShouldBe(["slug", "ownerEmail", "password"], ignoreOrder: true);
        problem.GetProperty("errorCodes").GetProperty("slug")[0].GetString().ShouldBe("tenant.slug_reserved");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static string UniqueSlug() => $"cafe-{Guid.NewGuid():N}"[..20];

    private static SignUpEndpoints.SignUpRequest SignUpBody(string slug) => new(
        BusinessName: "Moda Kahve",
        Slug: slug,
        DefaultCulture: "tr",
        Currency: "TRY",
        OwnerFullName: "Ayşe Tan",
        OwnerEmail: $"owner-{Guid.NewGuid():N}@armenu.test",
        Password: TestConfiguration.Password);
}
