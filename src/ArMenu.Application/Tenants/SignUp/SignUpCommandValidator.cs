using ArMenu.Application.Authentication;
using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Tenants.SignUp;

public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(command => command.BusinessName).NotEmpty().MaximumLength(Tenant.NameMaxLength);
        RuleFor(command => command.Slug).MustBeValid(CreateClaimableSlug);
        RuleFor(command => command.DefaultCulture).MustBeValid(CultureCode.Create);
        RuleFor(command => command.Currency).MustBeValid(Currency.Create);
        RuleFor(command => command.OwnerFullName).NotEmpty().MaximumLength(User.FullNameMaxLength);
        RuleFor(command => command.OwnerEmail).MustBeValid(Email.Create);
        RuleFor(command => command.Password).NotEmpty().Length(PasswordPolicy.MinLength, PasswordPolicy.MaxLength);
    }

    private static Result<TenantSlug> CreateClaimableSlug(string slug)
    {
        var created = TenantSlug.Create(slug);
        return created.IsSuccess && created.Value.IsReserved ? TenantErrors.SlugReserved : created;
    }
}
