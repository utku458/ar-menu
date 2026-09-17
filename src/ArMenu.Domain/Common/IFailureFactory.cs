namespace ArMenu.Domain.Common;

/// <summary>
/// Lets generic code, such as a validation pipeline behavior, create a failed <see cref="Result"/> or
/// <see cref="Result{TValue}"/> without knowing the concrete result type and without reflection.
/// </summary>
public interface IFailureFactory<TSelf>
    where TSelf : IFailureFactory<TSelf>
{
    static abstract TSelf Failure(Error error);
}
