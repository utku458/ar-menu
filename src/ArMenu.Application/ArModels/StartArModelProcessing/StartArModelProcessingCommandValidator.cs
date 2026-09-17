using FluentValidation;

namespace ArMenu.Application.ArModels.StartArModelProcessing;

public sealed class StartArModelProcessingCommandValidator : AbstractValidator<StartArModelProcessingCommand>
{
    public StartArModelProcessingCommandValidator()
    {
        RuleFor(command => command.UploadId).NotEmpty();
    }
}
