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

    [Theory]
    // Поля блока заголовка образца; значения сверены с байтами файла.
    [InlineData("{FIELD(\"MeasurementType\")}", "БЗП")]
    [InlineData("{FIELD(\"CoordinateSystem\")}", "XY")]
    [InlineData("{FIELD(\"ChamberType\")}", "БЗ")]
    [InlineData("{FIELD(\"DataAxis\")}", "Y")]
    [InlineData("{FIELD(\"SyncModeType\")}", "Простой")]
    [InlineData("{FIELD(\"AuxType\")}", "ТМАЗ 2-4")]
    [InlineData("{FIELD(\"AuxIdentifier\")}", "0526492")]
    [InlineData("{FIELD(\"IFBW\")}", "1000")]
    [InlineData("{FIELD(\"P\")}", "13")]
    [InlineData("{FIELD(\"DateStart\")}", "07.09.2026 15:10:11")]
    [InlineData("{FIELD(\"MeasurementDistance\")}", "0.0224983 м")]
    [InlineData("{FIELD(\"AuxPolarization\")}", "не задано")]
    [InlineData("{FIELD(\"SW1\")}", "не задано")]
    [InlineData("{FIELD(\"ZeroAz\")}", "не задано")]
    public void HeaderFieldIsSubstitutedFromTheFile(string field, string expected)
    {
        var result = Run(Header(field));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expected, File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
    }

    [Fact]
    public void HeaderFieldCanBeFormatted()
    {
        // FORMAT работает по числу, поэтому единица измерения уступает место формату.
        var result = Run(Header("{FORMAT(\"0.000\", {FIELD(\"MeasurementDistance\")})}"));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("0.022", File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
    }

    [Fact]
    public void DateFormatIsSetByADirective()
    {
        var result = Run("""
            [MDHEADER]
            {FILENAME("a.txt")}
            {GROUPBY("FREQ")}
            {NEWLINE("LF")}
            {DATEFORMAT("yyyy-MM-dd")}
            [/MDHEADER]
            {FIELD("DateStart")}
            """);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("2026-09-07", File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
    }

    [Fact]
    public void UnknownHeaderFieldNameIsAnError()
    {
        var result = Run(Header("{FIELD(\"POW\")}"));

        Assert.Equal(2, result.ExitCode);
        Assert.Empty(Directory.GetFiles(_target));
    }

    [Fact]
    public void FieldNameIsCaseInsensitive()
    {
        var result = Run(Header("{FIELD(\"ifbw\")}"));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1000", File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
    }

    [Fact]
    public void IndexOutsideAnAxisFailsTheFileInsteadOfWritingAStub()
    {
        // Файл с молчаливо пропущенным значением опаснее отсутствующего: он выглядит
        // рабочим. Поэтому ошибка подстановки обязана дойти до строки отчёта.
        var result = Run(Header("{FORMAT(\"0.0\", AMP({VALUE(0, 0, 0, 0, 0, 999, 0, 0)}))}"));

        Assert.Equal(1, result.ExitCode);
        Assert.All(result.Lines, line => Assert.Equal(1, line.Code));
        Assert.Empty(Directory.GetFiles(_target));
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("999", StringComparison.Ordinal));
    }

    /// <summary>Шаблон из одного поля: выводится только оно.</summary>
    private static string Header(string field) => $$"""
        [MDHEADER]
        {FILENAME("a.txt")}
        {GROUPBY("FREQ")}
        {NEWLINE("LF")}
        [/MDHEADER]
        {{field}}
        """;

    private FormatJobResult Run(string? template = null)
    {
        var path = Path.Combine(_directory, "template.md");
        File.WriteAllText(path, template ?? Template);

        return new FormattingService().Execute(new FormatRequest
        {
            TemplatePath = path,
            TargetDirectory = _target,
            Id = "1",
            SourcePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa_3.mtx"),
        });
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
