using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using FluentValidation;
using FluentValidation.Results;

namespace ArMenu.Application.Menus.Items;

internal static class MenuItemRules
{
    // ISO 4217 "XXX" (no currency): lets validators run the domain's amount rules before the tenant currency is known.
    private static readonly Currency AnyCurrency = Currency.Create("XXX").Value;

    public static Result<Money> ValidatePriceAmount(decimal amount) => Money.Create(amount, AnyCurrency);

    /// <summary>
    /// Checks each allergen and label code on its own field, then the contradictions between the two (reported on the
    /// labels, which are what the person has to change or explain).
    /// </summary>
    public static void AddDietaryRules<T>(
        this AbstractValidator<T> validator,
        Func<T, IReadOnlyList<string>?> allergens,
        Func<T, IReadOnlyList<string>?> labels)
    {
        validator.RuleFor(command => command).Custom((command, context) =>
        {
            // Both lists are checked, so every unknown code is reported at once; contradictions only between known codes.
            var allergensKnown = Check(allergens(command), "Allergens", DietaryInformation.ParseAllergen, context);
            var labelsKnown = Check(labels(command), "DietaryLabels", DietaryInformation.ParseLabel, context);
            if (!allergensKnown || !labelsKnown)
            {
                return;
            }

            var information = DietaryInformation.FromCodes(allergens(command), labels(command));
            if (information.IsFailure)
            {
                context.AddFailure(new ValidationFailure("DietaryLabels", information.Error.Description)
                {
                    ErrorCode = information.Error.Code,
                });
            }
        });
    }

    private static bool Check<TCode, T>(
        IReadOnlyList<string>? codes,
        string field,
        Func<string?, Result<TCode>> parse,
        ValidationContext<T> context)
    {
        var known = true;
        for (var index = 0; index < (codes?.Count ?? 0); index++)
        {
            var parsed = parse(codes![index]);
            if (parsed.IsFailure)
            {
                known = false;
                context.AddFailure(new ValidationFailure($"{field}[{index}]", parsed.Error.Description) { ErrorCode = parsed.Error.Code });
            }
        }

        return known;
    }
}
