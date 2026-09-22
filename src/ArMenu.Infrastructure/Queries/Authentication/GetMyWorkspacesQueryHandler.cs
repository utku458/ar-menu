using ArMenu.Application.Authentication.Queries.GetMyWorkspaces;
using ArMenu.Application.Team;
using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Persistence.Migrations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.Authentication;

/// <summary>
/// The only read across tenants in request handling. It names the user in a transaction-local setting that the
/// <c>own_memberships</c> policy reads, so PostgreSQL returns this user's memberships and nothing else, whatever the
/// query says. The tenant query filter is off because no single tenant applies.
/// </summary>
public sealed class GetMyWorkspacesQueryHandler(ArMenuDbContext dbContext)
    : IQueryHandler<GetMyWorkspacesQuery, Result<IReadOnlyList<WorkspaceResponse>>>
{
    public async ValueTask<Result<IReadOnlyList<WorkspaceResponse>>> Handle(GetMyWorkspacesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        var workspaces = await strategy.ExecuteAsync(async token =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
            var userId = query.UserId.Value.ToString();
            await dbContext.Database.ExecuteSqlRawAsync(
                $"SELECT set_config('{RowLevelSecurityMigrationExtensions.CurrentUserSettingName}', {{0}}, true)",
                [userId],
                token);

            var platform = TenantSlug.Platform;

            var rows = await dbContext.TenantMemberships
                .IgnoreQueryFilters([QueryFilters.Tenant])
                .AsNoTracking()
                .Where(membership => membership.UserId == query.UserId)
                .Join(
                    // The platform workspace is where the administrator's own session lives, not a business to switch to.
                    dbContext.Tenants.Where(tenant => tenant.Status == TenantStatus.Active && tenant.Slug != platform),
                    membership => membership.TenantId,
                    tenant => tenant.Id,
                    (membership, tenant) => new { tenant.Slug, tenant.Name, membership.Role })
                .OrderBy(row => row.Name)
                .ToListAsync(token);

            await transaction.CommitAsync(token);
            return rows;
        }, cancellationToken);

        return workspaces.Select(row => new WorkspaceResponse(row.Slug.Value, row.Name, row.Role.ToTeamRole())).ToList();
    }
}
