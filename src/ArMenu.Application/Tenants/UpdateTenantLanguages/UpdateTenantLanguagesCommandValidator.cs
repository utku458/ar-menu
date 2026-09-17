using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Tenants;
using FluentValidation;

namespace ArMenu.Application.Tenants.UpdateTenantLanguages;

public sealed class UpdateTenantLanguagesCommandValidator : AbstractValidator<UpdateTenantLanguagesCommand>
{
    public UpdateTenantLanguagesCommandValidator()
    {
        RuleFor(command => command.DefaultCulture).MustBeValid(CultureCode.Create);

        RuleFor(command => command.SupportedCultures)
            .NotEmpty()
            .Must(cultures => cultures.Count <= Tenant.MaxSupportedCultures).WithError(TenantErrors.SupportedCultureLimitReached);

        RuleForEach(command => command.SupportedCultures).MustBeValid(CultureCode.Create);

        RuleFor(command => command.DefaultCulture)
            .Must((command, defaultCulture) => command.SupportedCultures.Any(culture => Same(culture, defaultCulture)))
            .WithError(TenantErrors.CultureNotSupported)
            .When(command => command.SupportedCultures is not null);
    }

    private static bool Same(string? culture, string? other) =>
        CultureCode.Create(culture) is { IsSuccess: true } left &&
        CultureCode.Create(other) is { IsSuccess: true } right &&
        left.Value == right.Value;
}
