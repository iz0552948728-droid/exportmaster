using ExportMaster.Template.Parsing;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Template.Tests;

/// <summary>Экранирование и литералы.</summary>
public class TemplateLexerTests
{
    private const string Header = "[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\n";

    [Theory]
    [InlineData("{{", "{")]
    [InlineData("}}", "}")]
    [InlineData("{{}}", "{}")]
    [InlineData("a{{b}}c", "a{b}c")]
    [InlineData("{{{{", "{{")]
    public void DoubledBracesBecomeLiteralBraces(string source, string expected)
    {
        var result = TemplateParser.Parse(Header + source);

        Assert.False(result.HasErrors);
        Assert.Equal(expected, TemplateSplitterTests.BodyText(result));
    }

    [Fact]
    public void EscapedBracesAreNotConfusedWithFields()
    {
        var result = TemplateParser.Parse(Header + "{{FIELD}} — это текст, а {FIELD(\"M\",\"P\")} — поле");

        Assert.False(result.HasErrors);
        Assert.Equal("{FIELD} — это текст, а  — поле", TemplateSplitterTests.BodyText(result));
        Assert.Single(result.Document.Body.OfType<CallNode>());
    }

    [Fact]
    public void DoubledQuoteInsideStringBecomesSingleQuote()
    {
        var result = TemplateParser.Parse(Header + "{FORMAT(\"\"\"кавычки\"\"\",1)}");

        Assert.False(result.HasErrors);

        var call = Assert.Single(result.Document.Body.OfType<CallNode>());
        Assert.Equal("\"кавычки\"", Assert.IsType<StringArgument>(call.Arguments[0]).Value);
    }

    [Fact]
    public void BracesInsideStringDoNotAffectNesting()
    {
        var result = TemplateParser.Parse(Header + "{FORMAT(\"{не поле}\",1)}");

        Assert.False(result.HasErrors);

        var call = Assert.Single(result.Document.Body.OfType<CallNode>());
        Assert.Equal("{не поле}", Assert.IsType<StringArgument>(call.Arguments[0]).Value);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("42", 42)]
    [InlineData("-1", -1)]
    [InlineData("+3", 3)]
    [InlineData("1.5", 1.5)]
    [InlineData("-0.25", -0.25)]
    [InlineData("1.2e9", 1.2e9)]
    [InlineData("1E-3", 1E-3)]
    public void NumbersAreParsedIndependentlyOfLocale(string source, double expected)
    {
        var result = TemplateParser.Parse(Header + $"{{VALUE({source})}}");

        Assert.False(result.HasErrors);

        var call = Assert.Single(result.Document.Body.OfType<CallNode>());
        Assert.Equal(expected, Assert.IsType<NumberArgument>(call.Arguments[0]).Value);
    }

    [Fact]
    public void EmptyBodyProducesNoNodes()
    {
        var result = TemplateParser.Parse("[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\n");

        Assert.False(result.HasErrors);
        Assert.Empty(result.Document.Body);
    }
}
