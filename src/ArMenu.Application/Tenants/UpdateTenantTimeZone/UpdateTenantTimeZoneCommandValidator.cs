using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Tenants;
using FluentValidation;

namespace ArMenu.Application.Tenants.UpdateTenantTimeZone;

public sealed class UpdateTenantTimeZoneCommandValidator : AbstractValidator<UpdateTenantTimeZoneCommand>
{
    public UpdateTenantTimeZoneCommandValidator() => RuleFor(command => command.TimeZone).MustBeValid(TenantTimeZone.Create);
}
