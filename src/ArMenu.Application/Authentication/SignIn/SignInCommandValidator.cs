using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Authentication.SignIn;

public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    public SignInCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(Email.MaxLength);

        // No minimum length here: a policy change must never lock out users with older passwords.
        RuleFor(command => command.Password).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
    }
}
