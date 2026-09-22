using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Abstractions.Persistence;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Platform;

/// <summary>
/// Makes sure the platform has its workspace and its administrator, on every start and in every environment. Data, not
/// schema: it runs through the ordinary runtime pipeline, as the unprivileged runtime role, after the migration bundle
/// has already shaped the database.
/// </summary>
/// <remarks>
/// Idempotent, and it never takes anything over. If an ordinary account already holds the administrator's user name,
/// nothing is promoted: that would hand the platform to whoever registered the name first. It logs the conflict and
/// leaves the platform without an administrator until a different name is configured.
/// </remarks>
internal sealed partial class PlatformAdministratorSeeder(
    ITenantRepository tenants,
    IUserRepository users,
    ITenantMembershipRepository memberships,
    ITenantContextSetter tenantContextSetter,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IOptions<PlatformAdministratorOptions> options,
    ILogger<PlatformAdministratorSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var workspace = await tenants.FindBySlugAsync(TenantSlug.Platform, cancellationToken);

        if (workspace is null)
        {
            workspace = Tenant.CreatePlatform(
                CultureCode.Create(settings.DefaultCulture).Value,
                Currency.Create(settings.Currency).Value);
            tenants.Add(workspace);
            LogWorkspaceCreated(logger);
        }

        if (!await users.PlatformAdminExistsAsync(cancellationToken))
        {
            var userName = UserName.Create(settings.UserName);
            if (userName.IsFailure)
            {
                LogInvalidUserName(logger, settings.UserName);
            }
            else if (await users.UserNameExistsAsync(userName.Value, cancellationToken))
            {
                LogUserNameTaken(logger, userName.Value.Value);
            }
            else
            {
                var administrator = User.OpenPlatformAdmin(userName.Value, settings.FullName, passwordHasher.Hash(settings.InitialPassword));
                users.Add(administrator.Value);

                // A session may only exist for a membership — user_sessions says so with a foreign key, which is what
                // makes removing someone end their sessions. So the administrator is a member of the platform
                // workspace, its own and only one: it is a member of no business, and enters those with a short-lived
                // token instead (see EnterBusinessCommandHandler).
                tenantContextSetter.SetTenant(TenantInfo.From(workspace));
                memberships.Add(TenantMembership.Create(workspace.Id, administrator.Value.Id, TenantRole.Owner));
                LogAdministratorCreated(logger, userName.Value.Value);
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
        {
            // Two instances starting together can both find nothing and both insert; the unique indexes let one win.
            // The loser has nothing left to do: the platform is set up either way. Anything else is a real failure
            // and is left to stop the start-up, rather than leave a platform nobody can administer.
            LogConcurrentSeed(logger, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created the platform workspace")]
    private static partial void LogWorkspaceCreated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Created the platform administrator '{UserName}' with the configured initial password; change it after the first sign-in")]
    private static partial void LogAdministratorCreated(ILogger logger, string userName);

    [LoggerMessage(Level = LogLevel.Error, Message = "PlatformAdministrator:UserName '{UserName}' is not a valid user name; no administrator was created")]
    private static partial void LogInvalidUserName(ILogger logger, string userName);

    [LoggerMessage(Level = LogLevel.Error, Message = "An ordinary account already uses the user name '{UserName}'; it was not promoted. Configure a different PlatformAdministrator:UserName")]
    private static partial void LogUserNameTaken(ILogger logger, string userName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Another instance set the platform up at the same time")]
    private static partial void LogConcurrentSeed(ILogger logger, Exception exception);
}

public static class PlatformAdministratorSetup
{
    /// <summary>Runs <see cref="PlatformAdministratorSeeder"/> in a scope of its own, before the API takes requests.</summary>
    public static async Task EnsurePlatformAdministratorAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        await ActivatorUtilities
            .CreateInstance<PlatformAdministratorSeeder>(scope.ServiceProvider)
            .SeedAsync(cancellationToken);
    }
}
