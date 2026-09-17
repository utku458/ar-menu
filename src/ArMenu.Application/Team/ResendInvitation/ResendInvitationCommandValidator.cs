using ArMenu.Application.Common.Validation;
using ArMenu.Application.Emails;
using FluentValidation;

namespace ArMenu.Application.Team.ResendInvitation;

public sealed class ResendInvitationCommandValidator : AbstractValidator<ResendInvitationCommand>
{
    public ResendInvitationCommandValidator() =>
        RuleFor(command => command.Language).Must(language => language is null || EmailLanguage.IsSupported(language)).WithError(TeamErrors.LanguageUnsupported);
}
