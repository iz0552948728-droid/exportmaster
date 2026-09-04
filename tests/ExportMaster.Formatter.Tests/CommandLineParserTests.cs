using ExportMaster.Engine.Rendering;
using ExportMaster.Formatter.Cli;

namespace ExportMaster.Formatter.Tests;

/// <summary>Разбор командной строки (ТЗ п. 4.3.4).</summary>
public class CommandLineParserTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("exportmaster-cli").FullName;
    private readonly string _template;

    public CommandLineParserTests()
    {
        _template = Path.Combine(_directory, "template.md");
        File.WriteAllText(_template, "[MDHEADER]\n{FILENAME(\"a.csv\")}\n[/MDHEADER]\n");
    }

    [Fact]
    public void ParsesKeysFromRequirements()
    {
        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory, "/id", "42"]);

        Assert.True(result.Success);
        Assert.Equal(_template, result.Options!.TemplatePath);
        Assert.Equal(_directory, result.Options.TargetDirectory);
        Assert.Equal("42", result.Options.Id);
    }

    [Fact]
    public void AcceptsLongFormOfKeys()
    {
        // П. 4.9 допускает Linux, где /source неотличим от абсолютного пути.
        var result = CommandLineParser.Parse(["--md", _template, "--target", _directory]);

        Assert.True(result.Success);
    }

    [Fact]
    public void SourceIsSplitAtFirstColon()
    {
        // Путь в Windows содержит своё двоеточие: DATA:C:\data\matrix.bin.
        var data = Path.Combine(_directory, "matrix.bin");
        File.WriteAllBytes(data, [1, 2, 3]);

        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory, "/source", $"DATA:{data}"]);

        Assert.True(result.Success);
        Assert.Equal(data, result.Options!.Sources["DATA"]);
    }

    [Fact]
    public void SeveralSourcesAreCollected()
    {
        var first = CreateDataFile("a.bin");
        var second = CreateDataFile("b.bin");

        var result = CommandLineParser.Parse(
            ["/md", _template, "/target", _directory, "/source", $"DATA:{first}", "/source", $"MEAS:{second}"]);

        Assert.True(result.Success);
        Assert.Equal(2, result.Options!.Sources.Count);
    }

    [Fact]
    public void DuplicateAliasIsRejected()
    {
        var data = CreateDataFile("a.bin");

        var result = CommandLineParser.Parse(
            ["/md", _template, "/target", _directory, "/source", $"DATA:{data}", "/source", $"DATA:{data}"]);

        Assert.False(result.Success);
    }

    [Fact]
    public void MissingDataFileIsRejected()
    {
        var result = CommandLineParser.Parse(
            ["/md", _template, "/target", _directory, "/source", "DATA:нет-такого.bin"]);

        Assert.False(result.Success);
    }

    [Fact]
    public void TemplateKeyIsRequired()
    {
        var result = CommandLineParser.Parse(["/target", _directory]);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("/md", StringComparison.Ordinal));
    }

    [Fact]
    public void TargetKeyIsRequired()
    {
        var result = CommandLineParser.Parse(["/md", _template]);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("/target", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingTemplateFileIsRejected()
    {
        var result = CommandLineParser.Parse(["/md", "нет-такого.md", "/target", _directory]);

        Assert.False(result.Success);
    }

    [Fact]
    public void UnknownKeyIsRejected()
    {
        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory, "/unknown", "x"]);

        Assert.False(result.Success);
    }

    [Fact]
    public void KeyWithoutValueIsRejected()
    {
        var result = CommandLineParser.Parse(["/md"]);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    public void OverwriteKeyIsRead(string value, bool expected)
    {
        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory, "/overwrite", value]);

        Assert.True(result.Success);
        Assert.Equal(expected, result.Options!.Overwrite);
    }

    [Fact]
    public void OverwriteDefaultsToYes()
    {
        // Повторный прогон того же задания должен давать тот же результат.
        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory]);

        Assert.True(result.Options!.Overwrite);
    }

    [Fact]
    public void InvalidOverwriteValueIsRejected()
    {
        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory, "/overwrite", "maybe"]);

        Assert.False(result.Success);
    }

    [Fact]
    public void KeyForSingleFileIsAcceptedButNotUsedYet()
    {
        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory, "/key", "POL1=1"]);

        Assert.True(result.Success);
        Assert.Equal("POL1=1", result.Options!.Key);
    }

    [Fact]
    public void StubStyleIsRead()
    {
        var result = CommandLineParser.Parse(["/md", _template, "/target", _directory, "/stub", "sample"]);

        Assert.True(result.Success);
        Assert.Equal(StubStyle.Sample, result.Options!.StubStyle);
    }

    private string CreateDataFile(string name)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllBytes(path, [1, 2, 3]);
        return path;
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }
}
