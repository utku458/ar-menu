namespace ArMenu.Domain.Common;

/// <summary>
/// Outcome of an operation that can fail in an expected way. Makes failure part of the method signature
/// instead of hiding it behind exceptions used for control flow.
/// </summary>
public class Result : IFailureFactory<Result>
{
    private static readonly Result SuccessResult = new(error: null);

    private readonly Error? _error;

    private protected Result(Error? error) => _error = error;

    public bool IsSuccess => _error is null;

    public bool IsFailure => !IsSuccess;

    /// <exception cref="InvalidOperationException">The result is successful.</exception>
    public Error Error => _error ?? throw new InvalidOperationException("A successful result does not carry an error.");

    public static Result Success() => SuccessResult;

    public static Result<TValue> Success<TValue>(TValue value) => new(value);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static Result<TValue> Failure<TValue>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>(error);
    }

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that produces a <typeparamref name="TValue"/> when it succeeds.</summary>
public sealed class Result<TValue> : Result, IFailureFactory<Result<TValue>>
{
    private readonly TValue? _value;

    internal Result(TValue value)
        : base(error: null) => _value = value;

    internal Result(Error error)
        : base(error)
    {
    }

    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access the value of a failed result ({Error.Code}).");

    static Result<TValue> IFailureFactory<Result<TValue>>.Failure(Error error) => Failure<TValue>(error);

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
