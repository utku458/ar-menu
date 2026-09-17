using ArMenu.Application.Authentication;
using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Team.AcceptInvitation;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(100);
        RuleFor(command => command.FullName).MaximumLength(User.FullNameMaxLength);

        // The minimum applies to new accounts only (checked by the handler): existing passwords must keep working.
        RuleFor(command => command.Password).NotEmpty().MaximumLength(PasswordPolicy.MaxLength);
    }
}
