using ArMenu.Application.Authentication;
using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Users;
using FluentValidation;

namespace ArMenu.Application.Team.AddMember;

public sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(User.FullNameMaxLength);
        RuleFor(command => command.UserName).MustBeValid(UserName.Create);
        RuleFor(command => command.Password).NotEmpty().Length(PasswordPolicy.MinLength, PasswordPolicy.MaxLength);
        RuleFor(command => command.Role).Must(role => role is TeamRole.Manager or TeamRole.Staff)
            .WithMessage("Members can be managers or staff; a business has one owner.");
    }
}
