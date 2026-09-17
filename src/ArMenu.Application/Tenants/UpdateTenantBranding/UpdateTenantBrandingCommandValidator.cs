using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Media;
using ArMenu.Domain.Tenants;
using FluentValidation;

namespace ArMenu.Application.Tenants.UpdateTenantBranding;

public sealed class UpdateTenantBrandingCommandValidator : AbstractValidator<UpdateTenantBrandingCommand>
{
    public UpdateTenantBrandingCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithError(TenantErrors.NameRequired)
            .MaximumLength(Tenant.NameMaxLength).WithError(TenantErrors.NameTooLong);

        RuleFor(command => command.LogoPath!)
            .MustBeValid(AssetPath.Create)
            .When(command => command.LogoPath is not null);

        RuleFor(command => command.AccentColor!)
            .MustBeValid(BrandColor.Create)
            .When(command => command.AccentColor is not null);
    }
}
