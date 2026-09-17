using ArMenu.Domain.ArModels;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.ArModels.StartArModelProcessing;

/// <summary>Queues a completed GLB upload to become the 3D/AR model of a menu item.</summary>
public sealed record StartArModelProcessingCommand(MenuItemId ItemId, Guid UploadId) : ICommand<Result<ArModelProcessingId>>;
