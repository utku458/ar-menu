namespace ArMenu.Application.Team.Queries.GetTeam;

public sealed record TeamResponse(IReadOnlyList<TeamMemberResponse> Members, IReadOnlyList<PendingInvitationResponse> Invitations);

public sealed record TeamMemberResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    TeamRole Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LastSignedInAt);

/// <summary>A pending invitation. Once expired, its link no longer works; sending again makes a new one.</summary>
public sealed record PendingInvitationResponse(
    Guid Id,
    string Email,
    TeamRole Role,
    string InvitedBy,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    bool IsExpired);
