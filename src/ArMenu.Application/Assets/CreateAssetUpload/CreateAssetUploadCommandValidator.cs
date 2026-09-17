using ArMenu.Application.Common.Validation;
using FluentValidation;

namespace ArMenu.Application.Assets.CreateAssetUpload;

public sealed class CreateAssetUploadCommandValidator : AbstractValidator<CreateAssetUploadCommand>
{
    public CreateAssetUploadCommandValidator()
    {
        RuleFor(command => command.Kind).IsInEnum();

        RuleFor(command => command.ContentType)
            .Must((command, contentType) => AssetLimits.ContentTypes(command.Kind).Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithError(AssetErrors.ContentTypeNotAllowed);

        RuleFor(command => command.Size)
            .GreaterThan(0).WithError(AssetErrors.SizeInvalid)
            .Must((command, size) => size <= AssetLimits.MaxBytes(command.Kind))
            .WithErrorCode("asset.too_large")
            .WithMessage(command => AssetErrors.TooLarge(command.Kind).Description);
    }
}
