using ArMenu.Application.Authentication;
using FluentValidation;

namespace ArMenu.Application.Team.SetMemberPassword;

public sealed class SetMemberPasswordCommandValidator : AbstractValidator<SetMemberPasswordCommand>
{
    public SetMemberPasswordCommandValidator() =>
        RuleFor(command => command.NewPassword).NotEmpty().Length(PasswordPolicy.MinLength, PasswordPolicy.MaxLength);
}
