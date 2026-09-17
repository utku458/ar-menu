using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Common;

namespace ArMenu.Api.Http;

/// <summary>Translates application results into HTTP responses with RFC 9457 problem details.</summary>
internal static class ProblemResults
{
    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error is ValidationError validation)
        {
            var byField = validation.Errors.GroupBy(fieldError => ToJsonPath(fieldError.Field)).ToList();

            return TypedResults.ValidationProblem(
                errors: byField.ToDictionary(group => group.Key, group => group.Select(fieldError => fieldError.Message).ToArray()),
                title: validation.Description,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = validation.Code,
                    // Stable codes per field, so clients can localize messages instead of parsing English text.
                    ["errorCodes"] = byField.ToDictionary(group => group.Key, group => group.Select(fieldError => fieldError.Code).Distinct().ToArray()),
                });
        }

        return TypedResults.Problem(
            detail: error.Description,
            statusCode: StatusCodeFor(error.Type),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    public static IResult ToNoContent(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError,
    };

    // Validators report C# member paths ("Name[tr]"); clients send JSON ("name").
    private static string ToJsonPath(string field) =>
        string.IsNullOrEmpty(field) ? field : char.ToLowerInvariant(field[0]) + field[1..];
}
