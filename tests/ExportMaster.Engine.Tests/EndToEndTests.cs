using System.Globalization;
using ExportMaster.Engine.Jobs;
using ExportMaster.Template.Diagnostics;

namespace ExportMaster.Engine.Tests;

/// <summary>
/// Сквозной путь: шаблон и файл измерений на входе, готовые файлы на выходе.
/// </summary>
/// <remarks>
/// Значения сверены с независимым расчётом по байтам образца, а не с выводом
/// самой программы: иначе тест подтверждал бы лишь то, что она согласована сама
/// с собой.
/// </remarks>
public class EndToEndTests : IDisposable
{
    private const string Template = """
        [MDHEADER]
        {FILENAME("m_", {GROUPVALUE("FREQ")}, ".txt")}
        {GROUPBY("FREQ")}
        {NEWLINE("LF")}
        [/MDHEADER]
        {TABLE("POS", "DATA", "AMP", "0.00", ";")}
        """;

    private readonly string _directory = Directory.CreateTempSubdirectory("exportmaster-e2e").FullName;
    private readonly string _target;

    public EndToEndTests()
    {
        _target = Path.Combine(_directory, "out");
        Directory.CreateDirectory(_target);
    }

    [Fact]
    public void OneFilePerGroupAxisValue()
    {
        var result = Run();

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(3, result.Lines.Count);
        Assert.All(result.Lines, line => Assert.Equal(0, line.Code));

        Assert.Equal(
            new[] { "m_2.725GHz.txt", "m_2.775GHz.txt", "m_2.825GHz.txt" },
            result.Lines.Select(line => line.File));
    }

    [Fact]
    public void KeyNamesTheCurrentValueOfTheGroupAxis()
    {
        Assert.Equal("FREQ=2.725GHz", Run().Lines[0].Key);
    }

    [Fact]
    public void TableHasOneColumnPerColumnAxisPointAndOneRowPerRowAxisPoint()
    {
        Run();
        var lines = File.ReadAllLines(Path.Combine(_target, "m_2.725GHz.txt"));

        // POS даёт 53 колонки, DATA — 61 строку; плюс строка и колонка заголовков.
        Assert.Equal(62, lines.Length);
        Assert.Equal(54, lines[0].Split(';').Length);
    }

    [Fact]
    public void HeadersCarryAxisValuesFromTheFile()
    {
        Run();
        var lines = File.ReadAllLines(Path.Combine(_target, "m_2.725GHz.txt"));

        var columns = lines[0].Split(';');
        Assert.Equal(string.Empty, columns[0]);
        Assert.Equal("-1.3", columns[1]);
        Assert.Equal("-1.25", columns[2]);

        Assert.Equal("-1.5", lines[1].Split(';')[0]);
    }

    [Theory]
    // Значения посчитаны из байтов образца независимо: 20*log10(|z|).
    [InlineData("m_2.725GHz.txt", 1, 1, -63.19)]
    [InlineData("m_2.725GHz.txt", 1, 2, -61.06)]
    [InlineData("m_2.775GHz.txt", 1, 1, -62.60)]
    [InlineData("m_2.825GHz.txt", 1, 1, -58.14)]
    public void CellsMatchIndependentlyComputedAmplitudes(string file, int row, int column, double expected)
    {
        Run();
        var lines = File.ReadAllLines(Path.Combine(_target, file));
        var cell = lines[row].Split(';')[column];

        Assert.Equal(expected, double.Parse(cell, CultureInfo.InvariantCulture), 2);
    }

    [Fact]
    public void GroupByThatDoesNotLeaveTwoDimensionsIsRejected()
    {
        // В образце три измерения, значит осей разбиения должно быть ровно одна.
        var result = Run("""
            [MDHEADER]
            {FILENAME("a.txt")}
            {GROUPBY("FREQ", "POS")}
            [/MDHEADER]
            текст
            """);

        Assert.Equal(2, result.ExitCode);
        Assert.Empty(Directory.GetFiles(_target));
    }

    [Fact]
    public void GroupAxisAbsentFromTheFileIsRejected()
    {
        var result = Run("""
            [MDHEADER]
            {FILENAME("a.txt")}
            {GROUPBY("BEAM")}
            [/MDHEADER]
            текст
            """);

        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public void VectorFieldIsOnlyAWarningForNow()
    {
        // Чтение векторов ещё не сделано: файл формируется, значение — заглушка.
        var result = Run("""
            [MDHEADER]
            {FILENAME("a.txt")}
            {GROUPBY("FREQ")}
            [/MDHEADER]
            Измерение: {FIELD("MEAS", "NAME")}
            """);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            result.Diagnostics,
            d => d.Severity == DiagnosticSeverity.Warning);
    }

    private FormatJobResult Run(string? template = null)
    {
        var path = Path.Combine(_directory, "template.md");
        File.WriteAllText(path, template ?? Template);

        return new FormattingService().Execute(new FormatRequest
        {
            TemplatePath = path,
            TargetDirectory = _target,
            Id = "1",
            Sources = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["DATA"] = Path.Combine(AppContext.BaseDirectory, "samples", "aaa.mtx"),
            },
        });
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
