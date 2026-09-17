using System.Diagnostics.CodeAnalysis;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Memberships;

namespace ArMenu.Infrastructure.Authentication;

/// <summary>Invitation link secrets in the <see cref="OpaqueToken"/> format, keyed by invitation id.</summary>
internal sealed class InvitationTokenCodec : IInvitationTokenCodec
{
    public InvitationTokenSecret GenerateSecret()
    {
        var secret = OpaqueToken.GenerateSecret();
        return new InvitationTokenSecret(secret, InvitationTokenHash.Create(OpaqueToken.HashSecret(secret)).Value);
    }

    public string Encode(TenantInvitationId invitationId, InvitationTokenSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return OpaqueToken.Encode(invitationId.Value, secret.Value);
    }

    public bool TryDecode(string? token, out TenantInvitationId invitationId, [NotNullWhen(true)] out InvitationTokenHash? secretHash)
    {
        var decoded = OpaqueToken.TryDecode(token, out var id, out var hash);
        invitationId = decoded ? TenantInvitationId.From(id) : default;
        secretHash = decoded ? InvitationTokenHash.Create(hash).Value : null;
        return decoded;
    }
}
