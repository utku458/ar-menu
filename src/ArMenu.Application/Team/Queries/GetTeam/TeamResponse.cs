namespace ArMenu.Application.Team.Queries.GetTeam;

public sealed record TeamResponse(IReadOnlyList<TeamMemberResponse> Members, IReadOnlyList<PendingInvitationResponse> Invitations);

/// <summary>
/// A team member. <c>UserName</c> is set for accounts that sign in with one, whose <c>Email</c> is then only a
/// placeholder no mail reaches; those are also the accounts whose password the owner can reset.
/// </summary>
public sealed record TeamMemberResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    TeamRole Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LastSignedInAt,
    string? UserName = null);

/// <summary>A pending invitation. Once expired, its link no longer works; sending again makes a new one.</summary>
public sealed record PendingInvitationResponse(
    Guid Id,
    string Email,
    TeamRole Role,
    string InvitedBy,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    bool IsExpired);
