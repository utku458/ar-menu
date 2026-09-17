using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.ResendInvitation;

/// <summary>Sends a pending invitation again with a new link; the previous link stops working.</summary>
public sealed record ResendInvitationCommand(UserId SentBy, TenantInvitationId InvitationId, string? Language)
    : ICommand<Result<InvitationSent>>;
