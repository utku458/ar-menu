using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.InviteMember;

/// <summary>
/// Invites someone to the team of the tenant bound to the current scope, by e-mail. <see cref="InvitedBy"/> is the
/// signed-in owner; <see cref="Language"/> is the e-mail's language, <c>tr</c> or <c>en</c>, the business's default when
/// omitted.
/// </summary>
public sealed record InviteMemberCommand(UserId InvitedBy, string Email, TeamRole Role, string? Language)
    : ICommand<Result<InvitationSent>>;
