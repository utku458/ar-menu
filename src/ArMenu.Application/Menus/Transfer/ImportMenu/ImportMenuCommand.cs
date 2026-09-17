using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Menus.Transfer.ImportMenu;

/// <summary>
/// Applies a menu spreadsheet: rows with an id update that dish, rows without one add a dish. Dishes missing from the
/// file are left alone. All or nothing: one problem anywhere and nothing changes. With <c>DryRun</c>, it only reports
/// what would change, computed by the same code that applies it.
/// </summary>
public sealed record ImportMenuCommand(string Csv, bool DryRun) : ICommand<Result<MenuImportResponse>>
{
    public const int MaxRows = 2_000;

    /// <summary>1 MiB: a menu of the maximum size, in every language, fits many times over.</summary>
    public const int MaxBytes = 1_048_576;

    public override string ToString() => $"ImportMenuCommand {{ DryRun = {DryRun}, Length = {Csv.Length} }}";
}

/// <summary>What the file did or would do. <c>Applied</c> is false for a dry run and whenever <c>Errors</c> is not empty.</summary>
public sealed record MenuImportResponse(
    bool Applied,
    int Created,
    int Updated,
    int Unchanged,
    IReadOnlyList<MenuImportChange> Changes,
    IReadOnlyList<MenuImportError> Errors);

/// <summary>A dish the file adds (<c>created</c>) or changes (<c>updated</c>), with the columns that change.</summary>
public sealed record MenuImportChange(int Line, Guid ItemId, string Name, string Kind, IReadOnlyList<string> Fields);

/// <summary>A problem at a line (1 is the header) and column of the file; <c>Column</c> is null for the whole row.</summary>
public sealed record MenuImportError(int Line, string? Column, string Code, string Message);
