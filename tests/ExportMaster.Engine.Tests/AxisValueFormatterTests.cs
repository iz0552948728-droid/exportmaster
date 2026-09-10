using ExportMaster.Core;
using ExportMaster.Engine.Jobs;
using ExportMaster.Engine.Rendering;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Tests;

/// <summary>Текстовый вид значений осей.</summary>
public class AxisValueFormatterTests
{
    [Theory]
    [InlineData(2_725_000_000d, "2.725GHz")]
    [InlineData(1_000_000_000d, "1.000GHz")]
    [InlineData(12_500_000_000d, "12.500GHz")]
    public void GigahertzFromOneGigahertzAndAbove(double hertz, string expected)
    {
        Assert.Equal(expected, Format(Dimensions.FREQ, hertz));
    }

    [Theory]
    [InlineData(999_000_000d, "999.000MHz")]
    [InlineData(433_920_000d, "433.920MHz")]
    [InlineData(1_500_000d, "1.500MHz")]
    public void MegahertzBelowOneGigahertz(double hertz, string expected)
    {
        Assert.Equal(expected, Format(Dimensions.FREQ, hertz));
    }

    [Fact]
    public void NumberOfDecimalsComesFromTheTemplate()
    {
        Assert.Equal("2.73GHz", Format(Dimensions.FREQ, 2_725_000_000d, "0.00"));
        Assert.Equal("2.7GHz", Format(Dimensions.FREQ, 2_725_000_000d, "0.0"));
        Assert.Equal("3GHz", Format(Dimensions.FREQ, 2_725_000_000d, "0"));
    }

    [Fact]
    public void OtherAxesKeepTheirValuesUnlessAFormatIsGiven()
    {
        // Единицы известны только для частоты; остальные оси выводятся как записаны.
        Assert.Equal("-1.3", Format(Dimensions.POS, -1.3));
        Assert.Equal("-1.300", Format(Dimensions.POS, -1.3, "0.000"));
    }

    [Fact]
    public void StringAxisIsPassedThrough()
    {
        var axis = AxisDescriptor.FromStrings(Dimensions.BEAM, ["левый", "правый"]);
        var formatter = new AxisValueFormatter(new OutputSettings());

        Assert.Equal("правый", formatter.Format(axis, 1));
    }

    private static string Format(Dimensions axis, double value, string? format = null)
    {
        var settings = new OutputSettings
        {
            AxisFormats = format is null
                ? new Dictionary<Dimensions, string>()
                : new Dictionary<Dimensions, string> { [axis] = format },
        };

        return new AxisValueFormatter(settings).Format(AxisDescriptor.FromNumbers(axis, [value]), 0);
    }
}
