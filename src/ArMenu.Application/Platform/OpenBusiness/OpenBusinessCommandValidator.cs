using ArMenu.Application.Authentication;
using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Platform.OpenBusiness;

public sealed class OpenBusinessCommandValidator : AbstractValidator<OpenBusinessCommand>
{
    public OpenBusinessCommandValidator()
    {
        RuleFor(command => command.BusinessName).NotEmpty().MaximumLength(Tenant.NameMaxLength);
        RuleFor(command => command.Slug).MustBeValid(CreateClaimableSlug);
        RuleFor(command => command.DefaultCulture).MustBeValid(CultureCode.Create);
        RuleFor(command => command.Currency).MustBeValid(Currency.Create);
        RuleFor(command => command.OwnerFullName).NotEmpty().MaximumLength(User.FullNameMaxLength);
        RuleFor(command => command.OwnerUserName).MustBeValid(UserName.Create);
        RuleFor(command => command.OwnerPassword).NotEmpty().Length(PasswordPolicy.MinLength, PasswordPolicy.MaxLength);
    }

    // The same rule sign-up applied: the administrator cannot give a business a reserved handle either — least of all
    // the platform's own.
    private static Result<TenantSlug> CreateClaimableSlug(string slug)
    {
        var created = TenantSlug.Create(slug);
        return created.IsSuccess && created.Value.IsReserved ? TenantErrors.SlugReserved : created;
    }
}
