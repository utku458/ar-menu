using ArMenu.Application.Common.Validation;
using FluentValidation;

namespace ArMenu.Application.Assets.PublishAssetUpload;

public sealed class PublishAssetUploadCommandValidator : AbstractValidator<PublishAssetUploadCommand>
{
    public PublishAssetUploadCommandValidator()
    {
        RuleFor(command => command.UploadId).NotEmpty();
        RuleFor(command => command.Kind)
            .IsInEnum()
            .NotEqual(AssetKind.Model).WithError(AssetErrors.ModelRequiresProcessing);
    }
}
