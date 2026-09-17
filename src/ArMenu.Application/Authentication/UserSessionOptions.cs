using ArMenu.Domain.Sessions;

namespace ArMenu.Application.Authentication;

public sealed class UserSessionOptions
{
    public const string SectionName = "Authentication:Sessions";

    /// <summary>A session unused for this long expires. Defaults to 14 days.</summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromDays(14);

    /// <summary>Maximum session length regardless of activity. Defaults to 30 days.</summary>
    public TimeSpan AbsoluteLifetime { get; set; } = TimeSpan.FromDays(30);

    public SessionLifetime ToLifetime() => new(IdleTimeout, AbsoluteLifetime);
}
