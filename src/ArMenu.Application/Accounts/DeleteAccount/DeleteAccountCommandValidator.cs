using ArMenu.Application.Common.Validation;
using ArMenu.Application.Emails;
using ArMenu.Application.Team;
using FluentValidation;

namespace ArMenu.Application.Accounts.DeleteAccount;

public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(command => command.Password).NotEmpty();
        RuleFor(command => command.Language).Must(language => language is null || EmailLanguage.IsSupported(language)).WithError(TeamErrors.LanguageUnsupported);
    }
}
