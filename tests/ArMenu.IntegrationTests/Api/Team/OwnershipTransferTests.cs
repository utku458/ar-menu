using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Team;

public sealed class OwnershipTransferTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_owner_hands_the_business_over_and_both_see_their_new_roles_on_refresh()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        var manager = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Manager, Ct);
        using var ownerClient = await SignInAsync(restaurant, owner);
        var membershipId = await MembershipIdAsync(ownerClient, manager.UserId);

        using var response = await HandOverAsync(ownerClient, membershipId, TestConfiguration.Password);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(Ct));

        ownerClient.Authenticate(await RefreshAsync(ownerClient, restaurant));
        (await RoleAsync(ownerClient)).ShouldBe("Manager");
        using var managerClient = await SignInAsync(restaurant, manager);
        (await RoleAsync(managerClient)).ShouldBe("Owner");

        var members = (await managerClient.GetFromJsonAsync<JsonElement>("/api/v1/manage/team", Ct)).GetProperty("members");
        members.EnumerateArray().Count(member => member.GetProperty("role").GetString() == "Owner").ShouldBe(1);

        // The former owner's token is minutes old; the database decides that they own nothing now.
        using var back = await HandOverAsync(ownerClient, membershipId, TestConfiguration.Password);
        back.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_wrong_password_is_a_field_error_that_keeps_the_session_and_counts_towards_lockout()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        var staff = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Staff, Ct);
        using var ownerClient = await SignInAsync(restaurant, owner);
        var membershipId = await MembershipIdAsync(ownerClient, staff.UserId);

        for (var attempt = 1; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            using var wrong = await HandOverAsync(ownerClient, membershipId, "not-my-password");
            wrong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await wrong.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("password")[0].GetString().ShouldBe("account.password_incorrect");
        }

        using var locked = await HandOverAsync(ownerClient, membershipId, "not-my-password");
        (await locked.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("account.too_many_attempts");
        using var evenRight = await HandOverAsync(ownerClient, membershipId, TestConfiguration.Password);
        evenRight.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await RoleAsync(ownerClient)).ShouldBe("Owner");
    }

    [Fact]
    public async Task Ownership_goes_only_to_another_member_of_this_business()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var neighbour = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        await database.Services.SeedMemberAsync(neighbour.Tenant, TenantRole.Manager, Ct);
        using var ownerClient = await SignInAsync(restaurant, owner);
        var ownMembershipId = await MembershipIdAsync(ownerClient, owner.UserId);

        using var toSelf = await HandOverAsync(ownerClient, ownMembershipId, TestConfiguration.Password);
        (await toSelf.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("membership.already_owner");

        using var toStranger = await HandOverAsync(ownerClient, Guid.NewGuid(), TestConfiguration.Password);
        toStranger.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private async Task<HttpClient> SignInAsync(SeededTenant restaurant, SeededMember member)
    {
        var client = _factory.CreateBrowserClient();
        return client.Authenticate(await client.SignInAsync(restaurant.Tenant.Slug, member.Email));
    }

    private static Task<HttpResponseMessage> HandOverAsync(HttpClient client, Guid membershipId, string password) =>
        client.PostAsJsonAsync($"/api/v1/manage/team/members/{membershipId}/ownership", new { password }, Ct);

    private static async Task<Guid> MembershipIdAsync(HttpClient owner, UserId userId) =>
        (await owner.GetFromJsonAsync<JsonElement>("/api/v1/manage/team", Ct)).GetProperty("members").EnumerateArray()
            .Single(member => member.GetProperty("userId").GetGuid() == userId.Value).GetProperty("id").GetGuid();

    private static async Task<string> RoleAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/v1/me", Ct)).GetProperty("role").GetString()!;

    private static async Task<string> RefreshAsync(HttpClient client, SeededTenant restaurant)
    {
        using var response = await client.PostAsync($"/api/v1/tenants/{restaurant.Tenant.Slug}/auth/refresh", null, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("accessToken").GetString()!;
    }
}
