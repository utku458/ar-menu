using FluentValidation;

namespace ArMenu.Application.Team.TransferOwnership;

public sealed class TransferOwnershipCommandValidator : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipCommandValidator() => RuleFor(command => command.Password).NotEmpty();
}
