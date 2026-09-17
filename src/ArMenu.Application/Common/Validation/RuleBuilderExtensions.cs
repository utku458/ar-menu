using ArMenu.Domain.Common;
using FluentValidation;
using FluentValidation.Results;

namespace ArMenu.Application.Common.Validation;

public static class RuleBuilderExtensions
{
    /// <summary>
    /// Validates the input with the domain's own factory, so each rule is written once (in the domain) and the API still
    /// reports it per field, with the domain's error code.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, TProperty> MustBeValid<T, TProperty, TValue>(
        this IRuleBuilder<T, TProperty> ruleBuilder,
        Func<TProperty, Result<TValue>> factory)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);
        ArgumentNullException.ThrowIfNull(factory);

        return ruleBuilder.Custom((value, context) =>
        {
            var result = factory(value);
            if (result.IsFailure)
            {
                context.AddFailure(new ValidationFailure(context.PropertyPath, result.Error.Description)
                {
                    ErrorCode = result.Error.Code,
                });
            }
        });
    }

    /// <summary>Reports a failed rule with the code and description of an application or domain error.</summary>
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> ruleBuilder,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);
        ArgumentNullException.ThrowIfNull(error);

        return ruleBuilder.WithErrorCode(error.Code).WithMessage(error.Description);
    }
}
