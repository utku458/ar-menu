using FluentValidation;

namespace ArMenu.Application.Authentication.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Password).NotEmpty().Length(PasswordPolicy.MinLength, PasswordPolicy.MaxLength);
    }
}
