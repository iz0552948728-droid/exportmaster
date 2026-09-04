using ExportMaster.Engine.Jobs;

namespace ExportMaster.Engine.Tests;

/// <summary>Выполнение задания целиком: от шаблона до файла на диске.</summary>
public class FormattingServiceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("exportmaster-tests").FullName;
    private readonly string _target;

    public FormattingServiceTests()
    {
        // Каталог результата отделён от каталога шаблона, как при реальном запуске.
        _target = Path.Combine(_directory, "out");
        Directory.CreateDirectory(_target);
    }

    [Fact]
    public void ProducesFileAndReportsSuccess()
    {
        var result = Run("[MDHEADER]\n{FILENAME(\"report.csv\")}\n[/MDHEADER]\nЧастота;Уровень\n");

        Assert.Equal(0, result.ExitCode);

        var line = Assert.Single(result.Lines);
        Assert.Equal(0, line.Code);
        Assert.Equal("report.csv", line.File);
        Assert.Equal("Частота;Уровень\n", File.ReadAllText(Path.Combine(_target, "report.csv")));
    }

    [Fact]
    public void KeyListsAxesFromGroupBy()
    {
        var result = Run("[MDHEADER]\n{FILENAME(\"a.csv\")}\n{GROUPBY(POL1,FREQ)}\n[/MDHEADER]\nтекст");

        // Значения осей приходят из файлов данных, которые пока не читаются.
        Assert.Equal("POL1=*;FREQ=*", Assert.Single(result.Lines).Key);
    }

    [Fact]
    public void TemplateWithErrorsProducesNoFile()
    {
        var result = Run("[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\n{FIELD}");

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(1, Assert.Single(result.Lines).Code);
        Assert.Empty(Directory.GetFiles(_target));
    }

    [Fact]
    public void MissingTemplateFileIsReportedNotThrown()
    {
        // ТЗ п. 4.7: отказы не допускаются — неудача даёт строку с кодом 1.
        var result = new FormattingService().Execute(new FormatRequest
        {
            TemplatePath = Path.Combine(_directory, "нет-такого.md"),
            TargetDirectory = _target,
            Id = "1",
        });

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(1, Assert.Single(result.Lines).Code);
    }

    [Fact]
    public void ExistingFileIsOverwrittenByDefault()
    {
        // Повторный прогон того же задания должен давать тот же результат.
        Run("[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\nпервый");
        var result = Run("[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\nвторой");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("второй", File.ReadAllText(Path.Combine(_target, "a.csv")));
    }

    [Fact]
    public void ExistingFileIsKeptWhenOverwriteIsForbidden()
    {
        Run("[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\nпервый");
        var result = Run("[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\nвторой", overwrite: false);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal("первый", File.ReadAllText(Path.Combine(_target, "a.csv")));
    }

    [Fact]
    public void FileNameIsBuiltFromHeaderArguments()
    {
        var result = Run("[MDHEADER]\n{FILENAME(\"meas_\",\"01\",\".csv\")}\n[/MDHEADER]\nтекст");

        Assert.Equal("meas_01.csv", Assert.Single(result.Lines).File);
    }

    private FormatJobResult Run(string template, bool overwrite = true)
    {
        var path = Path.Combine(_directory, "template.md");
        File.WriteAllText(path, template);

        return new FormattingService().Execute(new FormatRequest
        {
            TemplatePath = path,
            TargetDirectory = _target,
            Id = "42",
            Overwrite = overwrite,
        });
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
