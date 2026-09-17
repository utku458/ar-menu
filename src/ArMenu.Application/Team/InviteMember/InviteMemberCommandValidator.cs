using ArMenu.Application.Common.Validation;
using ArMenu.Application.Emails;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Team.InviteMember;

public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(command => command.Email).MustBeValid(Email.Create);
        RuleFor(command => command.Role).Must(role => role is TeamRole.Manager or TeamRole.Staff).WithError(InvitationErrors.OwnerNotInvitable);
        RuleFor(command => command.Language).Must(language => language is null || EmailLanguage.IsSupported(language)).WithError(TeamErrors.LanguageUnsupported);
    }
}
