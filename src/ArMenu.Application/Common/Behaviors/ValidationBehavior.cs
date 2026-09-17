using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Common;
using FluentValidation;
using Mediator;

namespace ArMenu.Application.Common.Behaviors;

/// <summary>
/// Runs every FluentValidation validator of a message before its handler. Invalid messages never reach the handler;
/// they short-circuit into a failed result carrying all field errors (no exceptions used for control flow).
/// </summary>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : Result, IFailureFactory<TResponse>
{
    private readonly IValidator<TMessage>[] _validators = [.. validators];

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Length == 0)
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);
        var errors = new List<FieldError>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            errors.AddRange(result.Errors.Select(failure =>
                new FieldError(failure.PropertyName, NormalizeErrorCode(failure.ErrorCode), failure.ErrorMessage)));
        }

        return errors.Count == 0
            ? await next(message, cancellationToken)
            : TResponse.Failure(new ValidationError(errors));
    }

    // Built-in FluentValidation rules report codes like "NotEmptyValidator"; clients get the same snake_case style as
    // domain codes ("validation.not_empty").
    private static string NormalizeErrorCode(string errorCode)
    {
        const string BuiltInSuffix = "Validator";
        if (!errorCode.EndsWith(BuiltInSuffix, StringComparison.Ordinal))
        {
            return errorCode;
        }

        var ruleName = errorCode[..^BuiltInSuffix.Length];
        var snakeCase = string.Concat(ruleName.Select((character, index) =>
            char.IsUpper(character) && index > 0 ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));

        return "validation." + snakeCase;
    }
}
