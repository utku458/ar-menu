namespace ArMenu.Application.Abstractions.Persistence;

/// <summary>The data was changed by someone else between reading and saving it (optimistic concurrency).</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("The data was modified by another operation.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
