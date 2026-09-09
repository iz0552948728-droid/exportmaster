using ExportMaster.Engine.Jobs;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing;

namespace ExportMaster.Engine.Tests;

/// <summary>Проверка поля GROUPVALUE по списку осей отбора данных.</summary>
public class GroupValueValidatorTests
{
    [Fact]
    public void AxisFromGroupByIsAccepted()
    {
        var diagnostics = Validate(
            "{FILENAME(\"m_\", {GROUPVALUE(\"POL1\")}, \".txt\")}\n{GROUPBY(\"POL1\",\"SLIDER\")}",
            "текст");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void AxisOutsideGroupByIsRejected()
    {
        // Ось осталась внутри двумерной вырезки — текущего значения у неё нет.
        var diagnostics = Validate(
            "{FILENAME(\"a.txt\")}\n{GROUPBY(\"POL1\")}",
            "Луч: {GROUPVALUE(\"BEAM\")}");

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void UnknownAxisIsRejected()
    {
        var diagnostics = Validate("{FILENAME(\"a.txt\")}\n{GROUPBY(\"POL1\")}", "{GROUPVALUE(\"P1\")}");

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void SeveralAxisNamesAreRejected()
    {
        var diagnostics = Validate(
            "{FILENAME(\"a.txt\")}\n{GROUPBY(\"POL1\",\"BEAM\")}",
            "{GROUPVALUE(\"POL1\",\"BEAM\")}");

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void FieldIsFoundInsideNestedArguments()
    {
        // Поле может стоять глубоко внутри FILENAME или FORMAT — обход это учитывает.
        var diagnostics = Validate(
            "{FILENAME(\"a.txt\")}\n{GROUPBY(\"POL1\")}",
            "{FORMAT(\"0.00\", AMP({GROUPVALUE(\"BEAM\")}))}");

        Assert.Contains(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static List<Diagnostic> Validate(string header, string body)
    {
        var parsed = TemplateParser.Parse($"[MDHEADER]\n{header}\n[/MDHEADER]\n{body}");
        Assert.False(parsed.HasErrors);

        var diagnostics = new List<Diagnostic>();
        var settings = HeaderSettings.Parse(parsed.Document.Header, diagnostics);
        Assert.Empty(diagnostics);

        GroupValueValidator.Validate(parsed.Document, settings.GroupBy, diagnostics);
        return diagnostics;
    }
}
