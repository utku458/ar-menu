using ArMenu.Domain.Common;

namespace ArMenu.Domain.UnitTests.TestSupport;

internal static class ResultAssertions
{
    public static T ShouldSucceed<T>(this Result<T> result)
    {
        ((Result)result).ShouldSucceed();
        return result.Value;
    }

    public static void ShouldSucceed(this Result result)
    {
        if (result.IsFailure)
        {
            throw new ShouldAssertException($"Expected success, but the result failed with '{result.Error.Code}'.");
        }
    }

    public static void ShouldFailWith(this Result result, Error expected)
    {
        if (result.IsSuccess)
        {
            throw new ShouldAssertException($"Expected failure '{expected.Code}', but the result succeeded.");
        }

        result.Error.ShouldBe(expected);
    }
}
