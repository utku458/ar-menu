using ArMenu.Application.Menus.Transfer;

namespace ArMenu.Application.UnitTests.Menus;

public sealed class MenuCsvTests
{
    [Fact]
    public void Quoted_fields_keep_separators_quotes_and_line_breaks()
    {
        var csv = CsvTable.Write([["id", "name:tr"], ["", "Tavuk, pilav"], ["", "\"Ev\" usulü\nsıcak"]]);

        CsvTable.Read(csv).ShouldNotBeNull().Select(row => row.Cells).ShouldBe(
        [
            ["id", "name:tr"],
            ["", "Tavuk, pilav"],
            ["", "\"Ev\" usulü\nsıcak"],
        ]);
    }

    [Fact]
    public void Files_saved_by_Turkish_spreadsheets_are_read_too()
    {
        var rows = CsvTable.Read("﻿id;name:tr;price\r\n;Künefe;185,50\r\n;;\r\n").ShouldNotBeNull();

        rows.Count.ShouldBe(2, "a row of separators only is blank");
        rows[1].Line.ShouldBe(2);
        rows[1].Cells.ShouldBe(["", "Künefe", "185,50"]);
        MenuCsvColumns.ParsePrice(rows[1].Cells[2]).ShouldBe(185.50m);
    }

    [Fact]
    public void Line_numbers_count_the_lines_a_quoted_field_spans() =>
        CsvTable.Read("a,b\n\"x\ny\",1\nz,2\n").ShouldNotBeNull().Select(row => row.Line).ShouldBe([1, 2, 4]);

    [Fact]
    public void An_unclosed_quote_makes_the_file_unreadable() => CsvTable.Read("a,b\n\"x,1\n").ShouldBeNull();

    [Theory]
    [InlineData("=HYPERLINK(\"http://evil\")")]
    [InlineData("+90 555")]
    [InlineData("-5")]
    [InlineData("@SUM(A1)")]
    public void Cells_a_spreadsheet_would_run_as_formulas_are_written_as_text_and_read_back_unchanged(string value)
    {
        var written = CsvTable.Read(CsvTable.Write([[value]])).ShouldNotBeNull().Single().Cells.Single();

        written.ShouldStartWith("'");
        CsvTable.Unescape(written).ShouldBe(value);
    }

    [Theory]
    [InlineData("185", "185")]
    [InlineData(" 185.5 ", "185.5")]
    [InlineData("185,50", "185.50")]
    public void Prices_are_read_with_a_point_or_a_comma(string text, string expected) =>
        MenuCsvColumns.ParsePrice(text).ShouldBe(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("1.250,00")]
    [InlineData("1.250")]
    [InlineData("₺185")]
    [InlineData("-5")]
    [InlineData("")]
    public void Ambiguous_or_foreign_prices_are_refused(string text) => MenuCsvColumns.ParsePrice(text).ShouldBeNull();

    [Theory]
    [InlineData("evet", true)]
    [InlineData("Hayır", false)]
    [InlineData("TRUE", true)]
    [InlineData("0", false)]
    public void Flags_are_read_in_both_languages(string text, bool expected) => MenuCsvColumns.ParseFlag(text).ShouldBe(expected);
}
