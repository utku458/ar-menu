namespace ArMenu.Application.Abstractions.Persistence;

/// <summary>
/// Saving would violate a uniqueness rule. Handlers check uniqueness up front for a friendly error; this covers the race
/// where two requests pass that check at the same time.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException()
        : base("The data conflicts with existing data.")
    {
    }

    public UniqueConstraintViolationException(string message)
        : base(message)
    {
    }

    public UniqueConstraintViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
