namespace ArMenu.Domain.Sessions;

/// <summary>How long a sign-in session may live.</summary>
public sealed record SessionLifetime
{
    public SessionLifetime(TimeSpan idleTimeout, TimeSpan absoluteLifetime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(idleTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(absoluteLifetime, idleTimeout);

        IdleTimeout = idleTimeout;
        AbsoluteLifetime = absoluteLifetime;
    }

    /// <summary>A session unused for this long expires; every refresh pushes the deadline forward.</summary>
    public TimeSpan IdleTimeout { get; }

    /// <summary>Hard cap from sign-in, however actively the session is used. Forces periodic re-authentication.</summary>
    public TimeSpan AbsoluteLifetime { get; }
}
