using ArMenu.Application.Abstractions.Accounts;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Abstractions.Persistence;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure.Auditing;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Persistence.Configurations;
using ArMenu.Infrastructure.Persistence.Interceptors;
using ArMenu.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.Infrastructure.Accounts;

/// <summary>
/// Works in a scope bound to no tenant, inside one transaction, and names each business in turn with a
/// transaction-local <c>app.current_tenant</c>. Row-level security therefore still checks every read and write against
/// the business it concerns, and a failure halfway leaves every business as it was.
/// </summary>
internal sealed class AccountErasure(IServiceScopeFactory scopeFactory, TimeProvider timeProvider) : IAccountErasure
{
    public async Task<IReadOnlyList<AccountMembership>> ListMembershipsAsync(UserId userId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();

        return await InTransactionAsync(dbContext, async token =>
        {
            var memberships = await ReadMembershipsAsync(dbContext, userId, token);

            List<AccountMembership> result = [];
            foreach (var membership in memberships)
            {
                await SetCurrentTenantAsync(dbContext, membership.TenantId.Value.ToString(), token);
                var others = await CountOtherMembersAsync(dbContext, membership.TenantId, userId, token);
                result.Add(membership with { OtherMembers = others });
            }

            return result;
        }, cancellationToken);
    }

    public async Task EraseAsync(UserId userId, IReadOnlyCollection<TenantId> tenantsToClose, EmailMessage notice, string noticeTemplate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenantsToClose);

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();
        var now = timeProvider.GetUtcNow();

        await InTransactionAsync(dbContext, async token =>
        {
            dbContext.ChangeTracker.Clear();
            var memberships = await ReadMembershipsAsync(dbContext, userId, token, includeClosed: true);

            foreach (var membership in memberships)
            {
                var tenantId = membership.TenantId;
                await SetCurrentTenantAsync(dbContext, tenantId.Value.ToString(), token);

                if (tenantsToClose.Contains(tenantId) && await CountOtherMembersAsync(dbContext, tenantId, userId, token) > 0)
                {
                    throw new ConcurrencyConflictException("Someone joined a business that was about to close with the account.");
                }

                // The foreign key cascades to the person's sessions in this business.
                await dbContext.TenantMemberships
                    .IgnoreQueryFilters([QueryFilters.Tenant])
                    .Where(row => row.TenantId == tenantId && row.UserId == userId)
                    .ExecuteDeleteAsync(token);

                await RecordDepartureAsync(dbContext, tenantId, userId, now, token);
            }

            await SetCurrentTenantAsync(dbContext, string.Empty, token);

            var closing = await dbContext.Tenants.Where(tenant => tenantsToClose.Contains(tenant.Id)).ToListAsync(token);
            foreach (var tenant in closing)
            {
                tenant.Close(now);
            }

            var user = await dbContext.Users.SingleAsync(row => row.Id == userId, token);
            user.Erase(now);

            await dbContext.UserTokens.Where(token => token.UserId == userId).ExecuteDeleteAsync(token);
            scope.ServiceProvider.GetRequiredService<IEmailOutbox>().Add(notice, noticeTemplate);
            await dbContext.SaveChangesAsync(token);
            return true;
        }, cancellationToken);
    }

    private static async Task<List<AccountMembership>> ReadMembershipsAsync(
        ArMenuDbContext dbContext,
        UserId userId,
        CancellationToken cancellationToken,
        bool includeClosed = false)
    {
        // The own_memberships policy shows this person's rows in every business, and nothing else.
        await SetCurrentUserAsync(dbContext, userId, cancellationToken);

        var rows = await dbContext.TenantMemberships
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Join(
                dbContext.Tenants.Where(tenant => includeClosed || tenant.Status != TenantStatus.Closed),
                membership => membership.TenantId,
                tenant => tenant.Id,
                (membership, tenant) => new { tenant.Id, tenant.Slug, tenant.Name, membership.Role })
            .OrderBy(row => row.Name)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(row => new AccountMembership(row.Id, row.Slug.Value, row.Name, row.Role, OtherMembers: 0))];
    }

    private static Task<int> CountOtherMembersAsync(ArMenuDbContext dbContext, TenantId tenantId, UserId userId, CancellationToken cancellationToken) =>
        dbContext.TenantMemberships
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .CountAsync(membership => membership.TenantId == tenantId && membership.UserId != userId, cancellationToken);

    // Written directly: the scope is bound to no tenant, so the tracked write path (rightly) refuses tenant data.
    private static Task<int> RecordDepartureAsync(ArMenuDbContext dbContext, TenantId tenantId, UserId userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            $$"""
            INSERT INTO {{AuditLogEntryConfiguration.TableName}} (tenant_id, id, occurred_at, actor_user_id, action, subject_type, subject_id, subject_name, changes)
            VALUES ({0}, {1}, {2}, {3}, '{{AuditActions.Deleted}}', '{{AuditSubjects.Member}}', {3}, NULL, '[]'::jsonb)
            """,
            [tenantId.Value, Guid.CreateVersion7(now), now, userId.Value],
            cancellationToken);

    private static Task<int> SetCurrentTenantAsync(ArMenuDbContext dbContext, string tenantId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync($"SELECT set_config('{TenantSessionConnectionInterceptor.SettingName}', {{0}}, true)", [tenantId], cancellationToken);

    private static Task<int> SetCurrentUserAsync(ArMenuDbContext dbContext, UserId userId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync($"SELECT set_config('{RowLevelSecurityMigrationExtensions.CurrentUserSettingName}', {{0}}, true)", [userId.Value.ToString()], cancellationToken);

    private static async Task<T> InTransactionAsync<T>(ArMenuDbContext dbContext, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async token =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
            var result = await work(token);
            await transaction.CommitAsync(token);
            return result;
        }, cancellationToken);
    }
}
