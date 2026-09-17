using ArMenu.Application.Common.Validation;
using ArMenu.Application.Emails;
using ArMenu.Application.Team;
using FluentValidation;

namespace ArMenu.Application.Authentication.SendEmailVerification;

public sealed class SendEmailVerificationCommandValidator : AbstractValidator<SendEmailVerificationCommand>
{
    public SendEmailVerificationCommandValidator() =>
        RuleFor(command => command.Language).Must(language => language is null || EmailLanguage.IsSupported(language)).WithError(TeamErrors.LanguageUnsupported);
}
