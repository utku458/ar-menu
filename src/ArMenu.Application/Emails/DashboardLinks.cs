using Microsoft.Extensions.Options;

namespace ArMenu.Application.Emails;

/// <summary>
/// Addresses of the dashboard pages e-mails link to. Secrets travel in the fragment: browsers never send it to a
/// server, so it stays out of access logs and <c>Referer</c> headers.
/// </summary>
public sealed class DashboardLinks(IOptions<DashboardOptions> options)
{
    public Uri JoinTeam(string workspace, string token) => Page($"{workspace}/join", token);

    public Uri ResetPassword(string token) => Page("reset-password", token);

    public Uri VerifyEmail(string token) => Page("verify-email", token);

    private Uri Page(string path, string token) =>
        new(options.Value.Url ?? throw new InvalidOperationException($"{DashboardOptions.SectionName}:Url is not configured."), $"{path}#{token}");
}
