using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.Emails;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Memberships;

namespace ArMenu.Application.Team;

/// <summary>Sends an invitation's link, which exists only in the e-mail: the database keeps its hash.</summary>
public sealed class InvitationMailer(
    IEmailSender emailSender,
    IInvitationTokenCodec tokenCodec,
    DashboardLinks dashboard,
    ArMenuMetrics metrics)
{
    /// <returns>Whether the mail server accepted the message.</returns>
    public async Task<bool> SendAsync(
        TenantInvitation invitation,
        InvitationTokenSecret secret,
        TenantInfo tenant,
        string inviterName,
        string? language,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(tenant);

        var acceptUrl = dashboard.JoinTeam(tenant.Slug, tokenCodec.Encode(invitation.Id, secret));
        var message = InvitationEmail.Compose(
            invitation.Email.Value,
            tenant.Name,
            inviterName,
            invitation.Role,
            acceptUrl,
            EmailLanguage.Resolve(language, tenant.DefaultCulture));

        // Delivery happens after the invitation is saved, and a failure does not undo it: the business sends it again.
        var sent = await emailSender.TrySendAsync(message, cancellationToken);
        metrics.EmailAttempted(InvitationEmail.Template, sent);
        return sent;
    }

}
