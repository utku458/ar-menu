using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Authentication.Queries.GetMyWorkspaces;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.SignInWithUserName;

/// <remarks>
/// The order of the steps is the security of it:
/// <list type="number">
/// <item>Credentials are checked before anything about the account's businesses is looked up, so an unknown name, a
/// wrong password and a locked account all look the same from outside.</item>
/// <item>The business is found through <see cref="GetMyWorkspacesQuery"/> — deliberately the one cross-tenant read the
/// application already has, rather than a second one to keep safe.</item>
/// <item>Only then is the scope bound to that business, so the membership check and the new session run under its
/// row-level security exactly like a sign-in that named the business in its URL.</item>
/// </list>
/// </remarks>
public sealed class SignInWithUserNameCommandHandler(
    IUserRepository users,
    ITenantMembershipRepository memberships,
    ITenantLookup tenantLookup,
    ITenantContextSetter tenantContextSetter,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    IMediator mediator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SignInWithUserNameCommand, Result<WorkspaceSignInResult>>
{
    public async ValueTask<Result<WorkspaceSignInResult>> Handle(SignInWithUserNameCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow();
        var userName = UserName.Create(command.UserName);
        var user = userName.IsSuccess ? await users.GetByUserNameAsync(userName.Value, cancellationToken) : null;

        if (user is null || user.IsLockedOut(now))
        {
            // Same work, same answer as a wrong password: timing and responses reveal neither accounts nor lockouts.
            passwordHasher.VerifyDecoy(command.Password);
            return AuthenticationErrors.InvalidCredentials;
        }

        var verification = passwordHasher.Verify(user.PasswordHash, command.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RecordFailedSignIn(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthenticationErrors.InvalidCredentials;
        }

        var workspace = await FindWorkspaceAsync(user, cancellationToken);
        if (workspace is not { IsActive: true })
        {
            // A member of no active business has nowhere to sign in to. Said no differently from a wrong password:
            // the password was right, but that is not something to confirm to whoever typed it.
            return AuthenticationErrors.InvalidCredentials;
        }

        tenantContextSetter.SetTenant(workspace);

        var membership = await memberships.GetByUserIdAsync(user.Id, cancellationToken);
        if (membership is null)
        {
            return AuthenticationErrors.InvalidCredentials;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(command.Password));
        }

        user.RecordSuccessfulSignIn(now);
        var authentication = sessionIssuer.StartSession(user, membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkspaceSignInResult(authentication, workspace.Slug, user.IsPlatformAdmin);
    }

    /// <summary>
    /// The platform workspace for the administrator, which it is a member of like anyone is of their own; otherwise
    /// the member's first business by name. Someone in more than one business lands in the first and switches from
    /// there, as the dashboard already lets them.
    /// </summary>
    private async ValueTask<TenantInfo?> FindWorkspaceAsync(User user, CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return await tenantLookup.FindBySlugAsync(TenantSlug.Platform, cancellationToken);
        }

        var workspaces = await mediator.Send(new GetMyWorkspacesQuery(user.Id), cancellationToken);
        if (workspaces.IsFailure || workspaces.Value.Count == 0)
        {
            return null;
        }

        var slug = TenantSlug.Create(workspaces.Value[0].Slug);
        return slug.IsSuccess ? await tenantLookup.FindBySlugAsync(slug.Value, cancellationToken) : null;
    }
}
