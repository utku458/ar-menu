using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Authentication.SignInWithUserName;

/// <summary>
/// Signs a user-name account in without asking which business: the account decides. Members land in their business,
/// the platform administrator in the platform workspace.
/// </summary>
public sealed record SignInWithUserNameCommand(string UserName, string Password) : ICommand<Result<WorkspaceSignInResult>>
{
    public override string ToString() => "SignInWithUserNameCommand { *** }";
}

/// <param name="Authentication">The token pair, scoped to <paramref name="WorkspaceSlug"/> like every session.</param>
/// <param name="WorkspaceSlug">Where the session lives, which is also where the refresh cookie belongs.</param>
/// <param name="IsPlatformAdmin">Whether this is the platform administrator, who starts at the platform, not a business.</param>
public sealed record WorkspaceSignInResult(AuthenticationResult Authentication, string WorkspaceSlug, bool IsPlatformAdmin)
{
    public override string ToString() => $"WorkspaceSignInResult {{ WorkspaceSlug = {WorkspaceSlug} }}";
}
