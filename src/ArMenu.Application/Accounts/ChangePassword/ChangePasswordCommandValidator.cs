using ArMenu.Application.Authentication;
using FluentValidation;

namespace ArMenu.Application.Accounts.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        // No minimum on the current password: it may predate the policy — the platform administrator's initial
        // password does — and a rule that stopped it from being replaced would keep the weak one in place for good.
        RuleFor(command => command.CurrentPassword).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
        RuleFor(command => command.NewPassword).NotEmpty().Length(PasswordPolicy.MinLength, PasswordPolicy.MaxLength);
    }
}
