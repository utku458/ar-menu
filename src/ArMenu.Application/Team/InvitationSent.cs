namespace ArMenu.Application.Team;

/// <summary>
/// A saved invitation, pending whether or not the e-mail got out. When the e-mail was not sent, the mail server
/// did not accept it, and the invitation can be sent again.
/// </summary>
public sealed record InvitationSent(Guid InvitationId, bool EmailSent);
