using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Menus.Transfer.Queries.ExportMenu;

/// <summary>The whole menu as a spreadsheet to edit and import again: every dish, in menu order, in every language.</summary>
public sealed record ExportMenuQuery : IQuery<Result<MenuExport>>;

public sealed record MenuExport(string FileName, string Csv);
