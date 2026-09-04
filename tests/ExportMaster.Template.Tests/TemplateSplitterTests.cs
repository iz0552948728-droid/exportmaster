using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing;

namespace ExportMaster.Template.Tests;

/// <summary>Деление файла шаблона на заголовок и тело.</summary>
public class TemplateSplitterTests
{
    private const string MinimalHeader = "[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\n";

    [Fact]
    public void HeaderIsNotCopiedToOutput()
    {
        var result = TemplateParser.Parse(MinimalHeader + "Частота;Уровень\n");

        Assert.False(result.HasErrors);
        Assert.True(result.Document.HasHeader);
        Assert.Single(result.Document.Header);
        Assert.Equal("Частота;Уровень\n", BodyText(result));
    }

    [Fact]
    public void BodyStartsRightAfterClosingTag()
    {
        // Перевод строки, завершающий закрывающий тег, в тело не входит — иначе
        // каждый выходной файл начинался бы с пустой строки.
        var result = TemplateParser.Parse(MinimalHeader + "первая строка");

        Assert.Equal("первая строка", BodyText(result));
    }

    [Fact]
    public void ByteOrderMarkIsIgnored()
    {
        // Блокнот и Visual Studio ставят BOM самостоятельно; без этого правила
        // открывающий тег не совпал бы и заголовок уехал бы в результат.
        var result = TemplateParser.Parse('﻿' + MinimalHeader + "тело");

        Assert.False(result.HasErrors);
        Assert.Equal("тело", BodyText(result));
    }

    [Fact]
    public void BlankLinesBeforeOpeningTagAreAllowed()
    {
        var result = TemplateParser.Parse("\n   \n" + MinimalHeader + "тело");

        Assert.False(result.HasErrors);
        Assert.Equal("тело", BodyText(result));
    }

    [Theory]
    [InlineData("[mdheader]", "[/mdheader]")]
    [InlineData("[MdHeader]", "[/MdHeader]")]
    [InlineData("  [MDHEADER]  ", "\t[/MDHEADER]\t")]
    public void TagsAreCaseInsensitiveAndMayBeSurroundedByWhitespace(string opening, string closing)
    {
        var result = TemplateParser.Parse($"{opening}\n{{FILENAME(\"a.csv\")}}\n{closing}\nтело");

        Assert.False(result.HasErrors);
        Assert.Equal("тело", BodyText(result));
    }

    [Fact]
    public void MissingHeaderIsAnError()
    {
        var result = TemplateParser.Parse("просто текст без заголовка");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.MissingHeader);
        Assert.False(result.Document.HasHeader);
    }

    [Fact]
    public void UnterminatedHeaderIsAnError()
    {
        // Молчаливый откат к «файлу без заголовка» недопустим: служебные строки
        // уехали бы в результат заказчику незамеченными.
        var result = TemplateParser.Parse("[MDHEADER]\n{FILENAME(\"a.csv\")}\nтело без закрывающего тега");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.UnterminatedHeader);
    }

    [Fact]
    public void TagsInsideBodyAreOrdinaryText()
    {
        var result = TemplateParser.Parse(MinimalHeader + "[MDHEADER] в теле\n[/MDHEADER]");

        Assert.False(result.HasErrors);
        Assert.Equal("[MDHEADER] в теле\n[/MDHEADER]", BodyText(result));
    }

    [Fact]
    public void CarriageReturnLineFeedIsHandled()
    {
        var result = TemplateParser.Parse("[MDHEADER]\r\n{FILENAME(\"a.csv\")}\r\n[/MDHEADER]\r\nтело");

        Assert.False(result.HasErrors);
        Assert.Equal("тело", BodyText(result));
    }

    [Fact]
    public void NonFieldTextInHeaderIsAnError()
    {
        // Так ловятся опечатки в именах полей: без проверки они молча
        // считались бы текстом и терялись.
        var result = TemplateParser.Parse("[MDHEADER]\nFILENAME(\"a.csv\")\n[/MDHEADER]\nтело");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.UnexpectedTextInHeader);
    }

    internal static string BodyText(ParseResult result) =>
        string.Concat(result.Document.Body.OfType<Parsing.Ast.TextNode>().Select(node => node.Text));
}
