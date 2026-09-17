using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using FluentValidation;
using FluentValidation.Results;

namespace ArMenu.Application.Menus;

/// <summary>
/// Validation rules for menu texts, including the tenant-specific ones (default language present, only supported
/// languages), so a client learns about every problem of a request at once rather than one per round trip.
/// </summary>
internal static class MenuTextRules
{
    public static IRuleBuilderOptionsConditions<T, IReadOnlyDictionary<string, string>> MustBeMenuText<T>(
        this IRuleBuilder<T, IReadOnlyDictionary<string, string>> ruleBuilder,
        ITenantContext tenantContext,
        int maxLength,
        Error tooLongError) =>
        ruleBuilder.Custom((translations, context) =>
        {
            if (translations is not null)
            {
                Check(translations, tenantContext, maxLength, tooLongError, context);
            }
        });

    /// <summary>Like <see cref="MustBeMenuText{T}"/>, but a missing or empty set of translations is accepted.</summary>
    public static IRuleBuilderOptionsConditions<T, IReadOnlyDictionary<string, string>?> MustBeOptionalMenuText<T>(
        this IRuleBuilder<T, IReadOnlyDictionary<string, string>?> ruleBuilder,
        ITenantContext tenantContext,
        int maxLength,
        Error tooLongError) =>
        ruleBuilder.Custom((translations, context) =>
        {
            if (translations is { Count: > 0 })
            {
                Check(translations, tenantContext, maxLength, tooLongError, context);
            }
        });

    private static void Check<T>(
        IReadOnlyDictionary<string, string> translations,
        ITenantContext tenantContext,
        int maxLength,
        Error tooLongError,
        ValidationContext<T> context)
    {
        var text = MenuTranslations.Create(translations, tenantContext.RequireTenant());
        var error = text.IsFailure ? text.Error : text.Value.ExceedsLength(maxLength) ? tooLongError : null;

        if (error is not null)
        {
            context.AddFailure(new ValidationFailure(context.PropertyPath, error.Description) { ErrorCode = error.Code });
        }
    }
}
