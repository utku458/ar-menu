using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Media;
using FluentValidation;

namespace ArMenu.Application.Menus.Items.AttachMenuItemArModel;

public sealed class AttachMenuItemArModelCommandValidator : AbstractValidator<AttachMenuItemArModelCommand>
{
    public AttachMenuItemArModelCommandValidator()
    {
        RuleFor(command => command.GlbPath).MustBeValid(AssetPath.Create);
        RuleFor(command => command.SceneViewerGlbPath).MustBeValid(AssetPath.Create).When(command => command.SceneViewerGlbPath is not null);
        RuleFor(command => command.UsdzPath).MustBeValid(AssetPath.Create).When(command => command.UsdzPath is not null);
        RuleFor(command => command.PosterPath).MustBeValid(AssetPath.Create).When(command => command.PosterPath is not null);
    }
}
