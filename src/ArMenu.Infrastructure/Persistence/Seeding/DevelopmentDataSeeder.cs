using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArMenu.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds demo tenants, menus and staff accounts so isolation, localization and sign-in can be tried locally.
/// Writes go through the normal runtime pipeline (interceptors and row-level security), which doubles as a smoke test.
/// Each part is idempotent, so an existing development database is completed rather than duplicated.
/// </summary>
internal sealed partial class DevelopmentDataSeeder(
    IServiceScopeFactory scopeFactory,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<DevelopmentDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var buildMenu in DemoMenus.All)
        {
            var menu = buildMenu();
            var tenant = await SeedTenantAsync(menu, cancellationToken);
            await SeedMembersAsync(menu, tenant, cancellationToken);
        }
    }

    private async Task<TenantInfo> SeedTenantAsync(DemoMenu menu, CancellationToken cancellationToken)
    {
        // One scope per tenant: a scope is bound to exactly one tenant for its whole lifetime.
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();

        var existing = await dbContext.Tenants.AsNoTracking()
            .SingleOrDefaultAsync(tenant => tenant.Slug == menu.Tenant.Slug, cancellationToken);
        if (existing is not null)
        {
            return TenantInfo.From(existing);
        }

        var tenantInfo = TenantInfo.From(menu.Tenant);
        scope.ServiceProvider.GetRequiredService<ITenantContextSetter>().SetTenant(tenantInfo);

        dbContext.Tenants.Add(menu.Tenant);
        dbContext.MenuCategories.AddRange(menu.Categories);
        dbContext.MenuItems.AddRange(menu.Items);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogTenantSeeded(logger, tenantInfo.Slug, menu.Categories.Count, menu.Items.Count);
        return tenantInfo;
    }

    private async Task SeedMembersAsync(DemoMenu menu, TenantInfo tenant, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextSetter>().SetTenant(tenant);
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();

        foreach (var member in menu.Members)
        {
            var email = Email.Create(member.Email).Value;
            if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            {
                continue;
            }

            var user = User.Register(email, member.FullName, passwordHasher.Hash(DemoMenus.DemoPassword)).Value;
            user.MarkEmailVerified(timeProvider.GetUtcNow());
            dbContext.Users.Add(user);
            dbContext.TenantMemberships.Add(TenantMembership.Create(tenant.Id, user.Id, member.Role));
            await dbContext.SaveChangesAsync(cancellationToken);

            LogMemberSeeded(logger, member.Email, member.Role, tenant.Slug);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo tenant '{Slug}' with {CategoryCount} categories and {ItemCount} items")]
    private static partial void LogTenantSeeded(ILogger logger, string slug, int categoryCount, int itemCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded demo account {Email} as {Role} of '{Slug}'")]
    private static partial void LogMemberSeeded(ILogger logger, string email, TenantRole role, string slug);
}
