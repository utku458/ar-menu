using System.Text;

namespace ArMenu.Application.Menus.Transfer;

/// <summary>
/// RFC 4180 CSV, as spreadsheets write it: quoted fields may hold separators, quotes and line breaks. Reading accepts a
/// comma or, as spreadsheets in Turkish and most European locales save, a semicolon.
/// </summary>
internal static class CsvTable
{
    public static string Write(IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendJoin(',', row.Select(Quote)).Append("\r\n");
        }

        return builder.ToString();
    }

    /// <summary>The rows, each with its 1-based line number; <see langword="null"/> when a quoted field is never closed.</summary>
    public static IReadOnlyList<(int Line, string[] Cells)>? Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        text = text.TrimStart('﻿');
        var separator = DetectSeparator(text);

        List<(int, string[])> rows = [];
        List<string> cells = [];
        var cell = new StringBuilder();
        var line = 1;
        var rowLine = 1;
        var quoted = false;

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    cell.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    line += character == '\n' ? 1 : 0;
                    cell.Append(character);
                }
            }
            else if (character == '"' && cell.Length == 0)
            {
                quoted = true;
            }
            else if (character == separator)
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                cells.Add(cell.ToString());
                cell.Clear();
                AddRow(rows, cells, rowLine);
                line++;
                rowLine = line;
            }
            else
            {
                cell.Append(character);
            }
        }

        if (quoted)
        {
            return null;
        }

        if (cell.Length > 0 || cells.Count > 0)
        {
            cells.Add(cell.ToString());
            AddRow(rows, cells, rowLine);
        }

        return rows;
    }

    private static void AddRow(List<(int, string[])> rows, List<string> cells, int line)
    {
        // Blank lines, and rows a spreadsheet left as separators only, carry nothing.
        if (cells.Any(value => value.Length > 0))
        {
            rows.Add((line, [.. cells]));
        }

        cells.Clear();
    }

    private static char DetectSeparator(string text)
    {
        var header = text.AsSpan(0, text.IndexOfAny(['\r', '\n']) is var end and >= 0 ? end : text.Length);
        return header.Count(';') > header.Count(',') ? ';' : ',';
    }

    // A cell a spreadsheet would run as a formula (=, +, -, @) is written behind an apostrophe, which spreadsheets show
    // as text; reading removes it again. See OWASP "CSV Injection".
    private static string Quote(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', ';', '"', '\r', '\n']) >= 0 || value != value.Trim()
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }

    public static string Unescape(string value) =>
        value.Length > 1 && value[0] == '\'' && value[1] is '=' or '+' or '-' or '@' or '\t' or '\r' ? value[1..] : value;
}
