using FluentValidation;

namespace ArMenu.Application.Authentication.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator() => RuleFor(command => command.Token).NotEmpty().MaximumLength(100);
}
