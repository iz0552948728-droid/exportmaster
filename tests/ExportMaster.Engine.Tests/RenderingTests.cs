using ExportMaster.Engine.Jobs;
using ExportMaster.Engine.Rendering;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing;

namespace ExportMaster.Engine.Tests;

/// <summary>Формирование содержимого выходного файла.</summary>
public class RenderingTests
{
    private const string Header = "[MDHEADER]\n{FILENAME(\"a.csv\")}\n{NEWLINE(\"LF\")}\n[/MDHEADER]\n";

    [Fact]
    public void TextOutsideFieldsIsCopiedUnchanged()
    {
        Assert.Equal("Частота;Уровень\n", Render("Частота;Уровень\n"));
    }

    [Fact]
    public void DescriptiveStubShowsFieldAsWritten()
    {
        // Автор шаблона должен узнать в выводе то, что сам написал.
        Assert.Equal("‹FIELD(\"MEAS\", \"POW\")›", Render("{FIELD(\"MEAS\",\"POW\")}"));
    }

    [Fact]
    public void EscapedBracesReachOutputAsLiteralBraces()
    {
        Assert.Equal("{}", Render("{{}}"));
    }

    [Fact]
    public void FormatAppliesFormatStringToNumber()
    {
        Assert.Equal("3.14", Render("{FORMAT(\"0.00\",3.14159)}", StubStyle.Sample));
    }

    [Fact]
    public void FormatDescribesItselfWhenValueIsNotNumeric()
    {
        // Форматировать можно только число: заглушка-описание им не является.
        Assert.Equal(
            "‹FORMAT(\"0.00\", FIELD(\"M\", \"P\"))›",
            Render("{FORMAT(\"0.00\",{FIELD(\"M\",\"P\")})}"));
    }

    [Fact]
    public void DecimalSeparatorFollowsHeaderDirective()
    {
        var content = Render(
            "{FORMAT(\"0.00\",3.5)}",
            StubStyle.Sample,
            "[MDHEADER]\n{FILENAME(\"a.csv\")}\n{DECIMAL(\",\")}\n{NEWLINE(\"LF\")}\n[/MDHEADER]\n");

        Assert.Equal("3,50", content);
    }

    [Fact]
    public void NewLinesAreNormalizedToTheChosenKind()
    {
        // Шаблон мог быть сохранён с любым соглашением, результат должен быть предсказуем.
        var content = Render(
            "первая\r\nвторая\rтретья\n",
            StubStyle.Descriptive,
            "[MDHEADER]\n{FILENAME(\"a.csv\")}\n{NEWLINE(\"CRLF\")}\n[/MDHEADER]\n");

        Assert.Equal("первая\r\nвторая\r\nтретья\r\n", content);
    }

    /// <summary>Табличная подстановка.</summary>
    public class Table
    {
        [Fact]
        public void ProducesHeaderRowAndHeaderColumn()
        {
            // Значения осей приходят из данных, вписать их в шаблон нельзя —
            // заголовки печатает сама таблица.
            var lines = RenderTable().Split('\n');

            Assert.Equal(5, lines.Length);
            Assert.Equal(";‹FREQ#1›;‹FREQ#2›;‹FREQ#3›", lines[0]);
            Assert.StartsWith("‹DATA#1›;", lines[1], StringComparison.Ordinal);
        }

        [Fact]
        public void UsesDelimiterFromFifthArgument()
        {
            // Разделитель задаётся в каждой таблице: у разных таблиц одного файла
            // он может различаться, поэтому в заголовок шаблона его не выносят.
            var content = Render("{TABLE(\"FREQ\",\"DATA\",\"AMP\",\"0.00\",\"|\")}");

            Assert.Contains('|', content);
            Assert.DoesNotContain(';', content);
        }

        [Fact]
        public void FifthArgumentIsRequired()
        {
            // Молча подставить точку с запятой опаснее ошибки: файл получился бы
            // правдоподобным, но с чужим разделителем.
            var diagnostics = RenderWithDiagnostics("{TABLE(\"FREQ\",\"DATA\",\"AMP\",\"0.00\")}");

            Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void DoesNotEndWithNewLine()
        {
            // Перевод строки после таблицы ставит сам шаблон.
            Assert.False(RenderTable().EndsWith('\n'));
        }

        [Fact]
        public void CornerCellIsEmpty()
        {
            Assert.StartsWith(";", RenderTable(), StringComparison.Ordinal);
        }

        [Fact]
        public void CellsDifferFromEachOther()
        {
            // Одинаковые числа по всей таблице скрыли бы сбитые колонки.
            var cells = RenderTable(StubStyle.Sample)
                .Split('\n')
                .Skip(1)
                .SelectMany(line => line.Split(';').Skip(1))
                .ToList();

            Assert.True(cells.Distinct().Count() > 1);
        }

        private static string RenderTable(StubStyle style = StubStyle.Descriptive) =>
            Render("{TABLE(\"FREQ\",\"DATA\",\"AMP\",\"0.00\",\";\")}", style);
    }

    private static List<Diagnostic> RenderWithDiagnostics(string body)
    {
        var parsed = TemplateParser.Parse(Header + body);
        Assert.False(parsed.HasErrors);

        var diagnostics = new List<Diagnostic>();
        var settings = HeaderSettings.Parse(parsed.Document.Header, diagnostics);
        var renderer = new TemplateRenderer(new StubValueResolver(StubStyle.Descriptive));

        renderer.Render(parsed.Document.Body, new RenderContext(settings.Output), diagnostics);
        return diagnostics;
    }

    private static string Render(string body, StubStyle style = StubStyle.Descriptive, string? header = null)
    {
        var parsed = TemplateParser.Parse((header ?? Header) + body);
        Assert.False(parsed.HasErrors);

        var diagnostics = new List<Diagnostic>();
        var settings = HeaderSettings.Parse(parsed.Document.Header, diagnostics);
        var renderer = new TemplateRenderer(new StubValueResolver(style));

        return renderer.Render(parsed.Document.Body, new RenderContext(settings.Output), diagnostics);
    }
}
