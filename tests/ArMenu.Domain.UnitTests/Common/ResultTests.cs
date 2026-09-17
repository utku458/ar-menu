using ArMenu.Domain.Common;

namespace ArMenu.Domain.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error SomeError = Error.Validation("test.error", "Something went wrong.");

    [Fact]
    public void Success_carries_value_and_no_error()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_carries_error_and_refuses_to_expose_a_value()
    {
        Result<int> result = SomeError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
        Should.Throw<InvalidOperationException>(() => result.Value).Message.ShouldContain(SomeError.Code);
    }

    [Fact]
    public void Failure_requires_an_error()
    {
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));
    }
}
