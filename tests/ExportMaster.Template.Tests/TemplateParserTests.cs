using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Template.Tests;

/// <summary>Разбор специальных полей в дерево.</summary>
public class TemplateParserTests
{
    private const string Header = "[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\n";

    /// <summary>
    /// Приёмочный критерий этапа: первый пример специальных полей из ТЗ п. 4.4.3.3
    /// должен разбираться дословно, как записан в документе.
    /// </summary>
    [Fact]
    public void ParsesFirstExampleFromRequirements()
    {
        var result = Parse("{FORMAT(\"#.00 дБм\",{FIELD(\"MEAS\",\"POW\")})}");

        Assert.False(result.HasErrors);

        var format = SingleCall(result);
        Assert.Equal("FORMAT", format.Name);
        Assert.Equal(2, format.Arguments.Count);
        Assert.Equal("#.00 дБм", Assert.IsType<StringArgument>(format.Arguments[0]).Value);

        var field = Assert.IsType<CallArgument>(format.Arguments[1]).Call;
        Assert.Equal("FIELD", field.Name);
        Assert.Equal("MEAS", Assert.IsType<StringArgument>(field.Arguments[0]).Value);
        Assert.Equal("POW", Assert.IsType<StringArgument>(field.Arguments[1]).Value);
    }

    /// <summary>
    /// Второй пример из ТЗ п. 4.4.3.3. Функция AMP записана там без обрамляющих
    /// фигурных скобок, в отличие от таблицы синтаксиса в том же пункте: внутри
    /// списка аргументов обе записи равнозначны.
    /// </summary>
    [Fact]
    public void ParsesSecondExampleFromRequirements()
    {
        var result = Parse("{FORMAT(\"#.000 дБ\",AMP({VALUE(\"DATA\",0,0,1,1,0,0,0,0)}))}");

        Assert.False(result.HasErrors);

        var format = SingleCall(result);
        Assert.Equal("FORMAT", format.Name);
        Assert.Equal("#.000 дБ", Assert.IsType<StringArgument>(format.Arguments[0]).Value);

        var amp = Assert.IsType<CallArgument>(format.Arguments[1]).Call;
        Assert.Equal("AMP", amp.Name);

        var value = Assert.IsType<CallArgument>(Assert.Single(amp.Arguments)).Call;
        Assert.Equal("VALUE", value.Name);
        Assert.Equal(9, value.Arguments.Count);
        Assert.Equal("DATA", Assert.IsType<StringArgument>(value.Arguments[0]).Value);
        Assert.Equal(
            new double[] { 0, 0, 1, 1, 0, 0, 0, 0 },
            value.Arguments.Skip(1).Select(argument => Assert.IsType<NumberArgument>(argument).Value));
    }

    [Fact]
    public void BracedAndBareCallsAreEquivalentInsideArguments()
    {
        var braced = Parse("{FORMAT(\"0.00\",{AMP(1)})}");
        var bare = Parse("{FORMAT(\"0.00\",AMP(1))}");

        Assert.False(braced.HasErrors);
        Assert.False(bare.HasErrors);

        var fromBraced = Assert.IsType<CallArgument>(SingleCall(braced).Arguments[1]).Call;
        var fromBare = Assert.IsType<CallArgument>(SingleCall(bare).Arguments[1]).Call;

        Assert.Equal(fromBraced.Name, fromBare.Name);
        Assert.Equal(fromBraced.Arguments.Count, fromBare.Arguments.Count);
    }

    [Fact]
    public void TableFieldWithCornerArgumentIsParsed()
    {
        var result = Parse("{TABLE(\"FREQ\",\"DATA\",\"AMP\",\"0.00\",\"Гц\")}");

        Assert.False(result.HasErrors);

        var table = SingleCall(result);
        Assert.Equal("TABLE", table.Name);
        Assert.Equal(5, table.Arguments.Count);
    }

    [Fact]
    public void BareIdentifiersAreParsedAsNames()
    {
        // Список отбора данных: {GROUPBY(POL1,FREQ,CHANNEL,BEAM)}, ТЗ п. 4.4.5.
        var result = Parse("{GROUPBY(POL1,FREQ,CHANNEL,BEAM)}");

        Assert.False(result.HasErrors);

        var groupBy = SingleCall(result);
        Assert.Equal(
            new[] { "POL1", "FREQ", "CHANNEL", "BEAM" },
            groupBy.Arguments.Select(argument => Assert.IsType<IdentifierArgument>(argument).Name));
    }

    [Fact]
    public void FieldWithoutArgumentsIsParsed()
    {
        var result = Parse("{NOW()}");

        Assert.False(result.HasErrors);
        Assert.Empty(SingleCall(result).Arguments);
    }

    [Fact]
    public void FieldMaySpanSeveralLines()
    {
        var result = Parse("{TABLE(\n    \"FREQ\",\n    \"DATA\",\n    \"AMP\",\n    \"0.00\")}");

        Assert.False(result.HasErrors);
        Assert.Equal(4, SingleCall(result).Arguments.Count);
    }

    [Fact]
    public void TextAroundFieldIsPreserved()
    {
        var result = Parse("до;{FIELD(\"M\",\"P\")};после");

        Assert.False(result.HasErrors);
        Assert.Equal("до;;после", TemplateSplitterTests.BodyText(result));
    }

    private static ParseResult Parse(string body) => TemplateParser.Parse(Header + body);

    private static CallNode SingleCall(ParseResult result) =>
        Assert.Single(result.Document.Body.OfType<CallNode>());

    /// <summary>Замечания синтаксиса.</summary>
    public class Diagnostics
    {
        [Fact]
        public void UnterminatedFieldIsReported()
        {
            var result = Parse("{FIELD(\"MEAS\",\"POW\")");

            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.ExpectedClosingBrace);
        }

        [Fact]
        public void UnterminatedStringIsReported()
        {
            var result = Parse("{FIELD(\"MEAS)}");

            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.UnterminatedString);
        }

        [Fact]
        public void MissingFieldNameIsReported()
        {
            var result = Parse("{(\"a\")}");

            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.ExpectedFieldName);
        }

        [Fact]
        public void MissingParenthesisIsReported()
        {
            var result = Parse("{FIELD}");

            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.ExpectedOpeningParenthesis);
        }

        [Fact]
        public void UnbalancedClosingBraceIsReported()
        {
            var result = Parse("итог} готов");

            Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.UnbalancedClosingBrace);
        }

        [Fact]
        public void DiagnosticPointsAtLineAndColumn()
        {
            // Шаблоны пишутся вручную: сообщение обязано указывать место.
            var result = TemplateParser.Parse(Header + "первая строка\nвторая {FIELD}\n");

            var diagnostic = Assert.Single(
                result.Diagnostics,
                d => d.Code == DiagnosticCode.ExpectedOpeningParenthesis);

            Assert.Equal(5, diagnostic.Span.Line);
            Assert.Equal(14, diagnostic.Span.Column);
        }

        [Fact]
        public void SeveralErrorsAreCollectedInOneRun()
        {
            // Автор шаблона должен увидеть все ошибки сразу, а не по одной за прогон.
            var result = Parse("{FIELD} и {VALUE}");

            Assert.Equal(
                2,
                result.Diagnostics.Count(d => d.Code == DiagnosticCode.ExpectedOpeningParenthesis));
        }

        private static ParseResult Parse(string body) => TemplateParser.Parse(Header + body);
    }
}
