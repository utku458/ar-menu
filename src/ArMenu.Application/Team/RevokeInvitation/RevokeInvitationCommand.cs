using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.RevokeInvitation;

/// <summary>Withdraws a pending invitation; its link stops working.</summary>
public sealed record RevokeInvitationCommand(TenantInvitationId InvitationId) : ICommand<Result>;
