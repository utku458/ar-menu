using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.DeleteMenuCategory;

public sealed record DeleteMenuCategoryCommand(MenuCategoryId CategoryId) : ICommand<Result>;
