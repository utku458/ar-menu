using System.Diagnostics.CodeAnalysis;
using ArMenu.Domain.Memberships;

namespace ArMenu.Application.Abstractions.Authentication;

/// <summary>Creates and parses the opaque secrets of invitation links, of the form <c>{invitationId}.{secret}</c>.</summary>
public interface IInvitationTokenCodec
{
    /// <summary>A new cryptographically random secret and the hash under which it is stored.</summary>
    InvitationTokenSecret GenerateSecret();

    string Encode(TenantInvitationId invitationId, InvitationTokenSecret secret);

    /// <summary>Parses a token and hashes its secret. Returns <see langword="false"/> for anything malformed.</summary>
    bool TryDecode(string? token, out TenantInvitationId invitationId, [NotNullWhen(true)] out InvitationTokenHash? secretHash);
}

public sealed record InvitationTokenSecret(string Value, InvitationTokenHash Hash)
{
    public override string ToString() => "InvitationTokenSecret { *** }";
}
