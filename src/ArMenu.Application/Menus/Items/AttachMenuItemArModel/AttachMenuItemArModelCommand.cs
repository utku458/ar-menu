using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.AttachMenuItemArModel;

/// <summary>Attaches (or replaces) the 3D/AR model of an item using keys of already uploaded assets.</summary>
public sealed record AttachMenuItemArModelCommand(
    MenuItemId ItemId,
    string GlbPath,
    string? SceneViewerGlbPath,
    string? UsdzPath,
    string? PosterPath) : ICommand<Result>;
