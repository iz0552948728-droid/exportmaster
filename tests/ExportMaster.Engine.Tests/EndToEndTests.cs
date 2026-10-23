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
        {FILENAME("m_", {GROUPVALUE("BEAM")}, ".txt")}
        {GROUPBY("BEAM")}
        {NEWLINE("LF")}
        [/MDHEADER]
        {TABLE("FREQ", "DATA", "AMP", "0.00", ";")}
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
        Assert.Equal(2, result.Lines.Count);
        Assert.All(result.Lines, line => Assert.Equal(0, line.Code));

        // Имена лучей приходят из блока строк описателя, а не из номеров точек.
        Assert.Equal(
            new[] { "m_Луч 0.txt", "m_Луч 1.txt" },
            result.Lines.Select(line => line.File));
    }

    [Fact]
    public void KeyNamesTheCurrentValueOfTheGroupAxis()
    {
        Assert.Equal("BEAM=Луч 0", Run().Lines[0].Key);
    }

    [Fact]
    public void TableHasOneColumnPerColumnAxisPointAndOneRowPerRowAxisPoint()
    {
        Run();
        var lines = File.ReadAllLines(Path.Combine(_target, "m_Луч 0.txt"));

        // FREQ даёт 3 колонки, DATA — 1801 строку; плюс строка и колонка заголовков.
        Assert.Equal(1802, lines.Length);
        Assert.Equal(4, lines[0].Split(';').Length);
    }

    [Fact]
    public void HeadersCarryAxisValuesFromTheFile()
    {
        Run();
        var lines = File.ReadAllLines(Path.Combine(_target, "m_Луч 0.txt"));

        var columns = lines[0].Split(';');

        // Угловая ячейка: ось сбора данных Az, значит строки — градусы.
        Assert.Equal("град", columns[0]);
        Assert.Equal("20.000GHz", columns[1]);
        Assert.Equal("30.000GHz", columns[2]);

        Assert.Equal("-90", lines[1].Split(';')[0]);
        Assert.Equal("-89.9", lines[2].Split(';')[0]);
    }

    [Theory]
    // Значения посчитаны из байтов образца независимо: 20*log10(|z|).
    [InlineData("m_Луч 0.txt", 1, 1, -86.35)]
    [InlineData("m_Луч 0.txt", 1, 2, -105.38)]
    [InlineData("m_Луч 0.txt", 1, 3, -92.74)]
    [InlineData("m_Луч 0.txt", 2, 1, -83.82)]
    [InlineData("m_Луч 1.txt", 1, 1, -88.77)]
    [InlineData("m_Луч 1.txt", 1801, 3, -99.96)]
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
        // В образце три измерения, значит ось разбиения должна быть ровно одна.
        var result = Run("""
            [MDHEADER]
            {FILENAME("a.txt")}
            {GROUPBY("FREQ", "BEAM")}
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
            {GROUPBY("POS")}
            [/MDHEADER]
            текст
            """);

        Assert.Equal(2, result.ExitCode);
    }

    [Theory]
    // Поля блока заголовка образца; значения сверены с байтами файла.
    [InlineData("{FIELD(\"MeasurementType\")}", "Сечение")]
    [InlineData("{FIELD(\"CoordinateSystem\")}", "ЭлАз")]
    [InlineData("{FIELD(\"ChamberType\")}", "ДЗ")]
    [InlineData("{FIELD(\"SyncModeType\")}", "не задано")]
    [InlineData("{FIELD(\"SyncMode\")}", "не задано")]
    [InlineData("{FIELD(\"DataAxis\")}", "Az")]
    [InlineData("{FIELD(\"AuxType\")}", "1")]
    [InlineData("{FIELD(\"AuxIdentifier\")}", "1")]
    [InlineData("{FIELD(\"IFBW\")}", "не задано")]
    [InlineData("{FIELD(\"P\")}", "не задано")]
    [InlineData("{FIELD(\"DateStart\")}", "08.10.2026 12:33:13")]
    [InlineData("{FIELD(\"MeasurementDistance\")}", "0 град")]
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
        Assert.Equal("0.000", File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
    }

    [Fact]
    public void TableCornerIsEmptyWhenNeitherAxisIsTheDataAxis()
    {
        // Единица измерения относится к оси сбора данных; без неё подписывать нечего.
        var result = Run("""
            [MDHEADER]
            {FILENAME("a.txt")}
            {GROUPBY("DATA")}
            {NEWLINE("LF")}
            [/MDHEADER]
            {TABLE("FREQ", "BEAM", "AMP", "0.00", ";")}
            """);

        Assert.Equal(0, result.ExitCode);
        var lines = File.ReadAllLines(Path.Combine(_target, "a.txt"));

        Assert.Equal(string.Empty, lines[0].Split(';')[0]);
    }

    [Fact]
    public void UnsetFieldInsideFormatIsPrintedInWords()
    {
        // ТЗ п. 4.4.7: значение NaN выводится как «не задано». Строка формата при
        // этом ни при чём — форматировать нечего, и это не ошибка шаблона.
        var result = Run(Header("{FORMAT(\"0.0 Гц\", {FIELD(\"IFBW\")})}"));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("не задано", File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
    }

    [Fact]
    public void DateFormatIsSetByADirective()
    {
        var result = Run("""
            [MDHEADER]
            {FILENAME("a.txt")}
            {GROUPBY("BEAM")}
            {NEWLINE("LF")}
            {DATEFORMAT("yyyy-MM-dd")}
            [/MDHEADER]
            {FIELD("DateStart")}
            """);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("2026-10-08", File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
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
        Assert.Equal("не задано", File.ReadAllText(Path.Combine(_target, "a.txt")).TrimEnd('\n'));
    }

    [Fact]
    public void IndexOutsideAnAxisFailsTheFileInsteadOfWritingAStub()
    {
        // Файл с молчаливо пропущенным значением опаснее отсутствующего: он выглядит
        // рабочим. Поэтому ошибка подстановки обязана дойти до строки отчёта.
        var result = Run(Header("{FORMAT(\"0.0\", AMP({VALUE(0, 0, 0, 0, 9999, 0, 0, 0)}))}"));

        Assert.Equal(1, result.ExitCode);
        Assert.All(result.Lines, line => Assert.Equal(1, line.Code));
        Assert.Empty(Directory.GetFiles(_target));
        Assert.Contains(result.Diagnostics, d => d.Message.Contains("9999", StringComparison.Ordinal));
    }

    /// <summary>Шаблон из одного поля: выводится только оно.</summary>
    private static string Header(string field) => $$"""
        [MDHEADER]
        {FILENAME("a.txt")}
        {GROUPBY("BEAM")}
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
            SourcePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa_6.mtx"),
        });
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
