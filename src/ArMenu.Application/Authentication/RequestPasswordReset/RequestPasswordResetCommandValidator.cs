using ArMenu.Application.Common.Validation;
using ArMenu.Application.Emails;
using ArMenu.Application.Team;
using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Authentication.RequestPasswordReset;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Email).MustBeValid(Email.Create);
        RuleFor(command => command.Language).Must(language => language is null || EmailLanguage.IsSupported(language)).WithError(TeamErrors.LanguageUnsupported);
    }
}
