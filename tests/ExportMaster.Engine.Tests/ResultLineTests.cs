using ExportMaster.Engine.Jobs;

namespace ExportMaster.Engine.Tests;

/// <summary>Протокол стандартного вывода (ТЗ п. 4.3.3).</summary>
public class ResultLineTests
{
    [Fact]
    public void LineHasFiveFieldsSeparatedByTabs()
    {
        var line = ResultLine.Success("42", "POL1=1", "report.csv").Format();

        Assert.Equal(new[] { "42", "0", "POL1=1", "report.csv", "" }, line.Split('\t'));
    }

    [Fact]
    public void FailureCarriesCodeOneAndMessage()
    {
        var line = ResultLine.Failure("42", string.Empty, "a.csv", "не вышло").Format().Split('\t');

        Assert.Equal("1", line[1]);
        Assert.Equal("не вышло", line[4]);
    }

    [Theory]
    [InlineData("сообщение\tс табуляцией")]
    [InlineData("сообщение\nс переводом строки")]
    [InlineData("сообщение\r\nс возвратом каретки")]
    public void SeparatorsInsideFieldsAreNeutralized(string message)
    {
        // Табуляция или перевод строки внутри поля сдвинули бы колонки
        // и сломали разбор на стороне заказчика.
        var line = ResultLine.Failure("42", string.Empty, "a.csv", message).Format();

        Assert.Equal(5, line.Split('\t').Length);
        Assert.DoesNotContain('\n', line);
    }
}
