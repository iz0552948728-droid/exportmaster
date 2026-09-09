using System.Text;
using ExportMaster.Core;
using ExportMaster.Engine.Jobs;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing;

namespace ExportMaster.Engine.Tests;

/// <summary>Директивы заголовка шаблона.</summary>
public class HeaderSettingsTests
{
    [Fact]
    public void DefaultsMatchAgreedSettings()
    {
        var (settings, diagnostics) = Parse("{FILENAME(\"a.csv\")}");

        Assert.Empty(diagnostics);
        Assert.Equal(".", settings.Output.DecimalSeparator);
        Assert.Equal(Environment.NewLine, settings.Output.NewLine);
    }

    [Fact]
    public void DefaultEncodingIsUtf8WithoutByteOrderMark()
    {
        var (settings, _) = Parse("{FILENAME(\"a.csv\")}");

        Assert.Empty(settings.Output.Encoding.GetPreamble());
    }

    [Fact]
    public void EncodingDirectiveTurnsOnByteOrderMark()
    {
        // Excel при двойном клике по csv без BOM покажет кириллицу как мусор.
        var (settings, _) = Parse("{FILENAME(\"a.csv\")}{ENCODING(\"UTF-8-BOM\")}");

        Assert.NotEmpty(settings.Output.Encoding.GetPreamble());
    }

    [Fact]
    public void SingleByteCyrillicEncodingIsSupported()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var (settings, diagnostics) = Parse("{FILENAME(\"a.csv\")}{ENCODING(\"CP1251\")}");

        Assert.Empty(diagnostics);
        Assert.Equal(1251, settings.Output.Encoding.CodePage);
    }

    [Fact]
    public void GroupByAcceptsAxisNames()
    {
        var (settings, diagnostics) = Parse("{FILENAME(\"a.csv\")}{GROUPBY(POL1,FREQ,CHANNEL,BEAM)}");

        Assert.Empty(diagnostics);
        Assert.Equal(
            new[] { Dimensions.POL1, Dimensions.FREQ, Dimensions.CHANNEL, Dimensions.BEAM },
            settings.GroupBy);
    }

    [Fact]
    public void UnknownAxisIsRejected()
    {
        // P1 из примера ТЗ заменено на POL1: имена берутся из перечисления Dimensions.
        var (_, diagnostics) = Parse("{FILENAME(\"a.csv\")}{GROUPBY(P1)}");

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void MissingFileNameIsAnError()
    {
        var (_, diagnostics) = Parse("{GROUPBY(FREQ)}");

        Assert.Contains(diagnostics, d => d.Code == DiagnosticCode.MissingHeader);
    }

    [Fact]
    public void UnknownDirectiveIsOnlyAWarning()
    {
        // Состав директив ещё пополняется — отказываться формировать файл рано.
        var (_, diagnostics) = Parse("{FILENAME(\"a.csv\")}{SOMETHING(\"x\")}");

        Assert.All(diagnostics, d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
    }

    [Fact]
    public void DelimiterIsNoLongerAHeaderDirective()
    {
        // Разделитель ячеек переехал в пятый аргумент TABLE: у разных таблиц
        // одного файла он может различаться.
        var (_, diagnostics) = Parse("{FILENAME(\"a.csv\")}{DELIMITER(\";\")}");

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void GroupByAcceptsAxisNamesAsStrings()
    {
        var (settings, diagnostics) = Parse("{FILENAME(\"a.csv\")}{GROUPBY(\"POL1\",\"SLIDER\")}");

        Assert.Empty(diagnostics);
        Assert.Equal(new[] { Dimensions.POL1, Dimensions.SLIDER }, settings.GroupBy);
    }

    [Fact]
    public void UnknownNewLineKindIsRejected()
    {
        var (_, diagnostics) = Parse("{FILENAME(\"a.csv\")}{NEWLINE(\"CR\")}");

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static (HeaderSettings Settings, List<Diagnostic> Diagnostics) Parse(string header)
    {
        var parsed = TemplateParser.Parse($"[MDHEADER]\n{header}\n[/MDHEADER]\n");
        Assert.False(parsed.HasErrors);

        var diagnostics = new List<Diagnostic>();
        return (HeaderSettings.Parse(parsed.Document.Header, diagnostics), diagnostics);
    }
}
