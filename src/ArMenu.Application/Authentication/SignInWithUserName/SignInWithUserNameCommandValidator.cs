using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Authentication.SignInWithUserName;

public sealed class SignInWithUserNameCommandValidator : AbstractValidator<SignInWithUserNameCommand>
{
    public SignInWithUserNameCommandValidator()
    {
        // Presence and size only. Whether the name is well formed is not the validator's to say: an ill-formed name
        // simply matches no account, and answering it any differently would tell a guesser which names can exist.
        RuleFor(command => command.UserName).NotEmpty().MaximumLength(UserName.MaxLength);

        // No minimum length here: a policy change must never lock out users with older passwords.
        RuleFor(command => command.Password).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
    }
}
