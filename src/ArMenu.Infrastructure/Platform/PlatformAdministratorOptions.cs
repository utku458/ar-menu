namespace ArMenu.Infrastructure.Platform;

/// <summary>The platform administrator's first sign-in, used only while no administrator exists yet.</summary>
/// <remarks>
/// Once the account exists these values are never read again: changing them does not change the password, the
/// administrator does that from the dashboard. Override them per deployment with <c>PlatformAdministrator__UserName</c>
/// and <c>PlatformAdministrator__InitialPassword</c>.
/// </remarks>
public sealed class PlatformAdministratorOptions
{
    public const string SectionName = "PlatformAdministrator";

    public string UserName { get; set; } = "admin";

    /// <summary>
    /// Shorter than the password policy allows anyone to choose (see <c>PasswordPolicy</c>), by the platform owner's
    /// decision; it is set without that check because it is not chosen through the policy's front door. On a public
    /// address it is a known credential, so set a different one per deployment, or change it at the first sign-in.
    /// </summary>
    public string InitialPassword { get; set; } = "admin1234";

    public string FullName { get; set; } = "Platform Administrator";

    /// <summary>Language and currency of the platform workspace; nothing a guest ever sees.</summary>
    public string DefaultCulture { get; set; } = "tr";

    public string Currency { get; set; } = "TRY";
}
