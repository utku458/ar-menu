using ArMenu.Application.Authentication;
using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Team.AcceptInvitation;

/// <summary>
/// Joins the team of the tenant bound to the current scope (the workspace in the URL) through an invitation link, and
/// signs the new member in. Someone with an account confirms it is theirs with their password; anyone else creates an
/// account, the invitation link having proven they control the address.
/// <see cref="FullName"/> is required when the invited address has no account yet, and ignored otherwise.
/// </summary>
public sealed record AcceptInvitationCommand(string Token, string? FullName, string Password) : ICommand<Result<AuthenticationResult>>
{
    public override string ToString() => "AcceptInvitationCommand { *** }";
}
