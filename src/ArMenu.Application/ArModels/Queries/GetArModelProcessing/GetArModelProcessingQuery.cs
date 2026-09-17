using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.ArModels.Queries.GetArModelProcessing;

/// <summary>The latest model processing of a menu item: its progress, or the report and warnings once it finished.</summary>
public sealed record GetArModelProcessingQuery(MenuItemId ItemId) : IQuery<Result<ArModelProcessingResponse>>;
