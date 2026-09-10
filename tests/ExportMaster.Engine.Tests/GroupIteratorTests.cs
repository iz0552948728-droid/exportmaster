using ExportMaster.Core;
using ExportMaster.Engine.Data;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Tests;

/// <summary>Разбиение матрицы по осям.</summary>
public class GroupIteratorTests
{
    private static MatrixFile Sample =>
        MatrixReader.Read(Path.Combine(AppContext.BaseDirectory, "samples", "aaa.mtx"));

    [Fact]
    public void OneSlicePerCombinationOfGroupAxes()
    {
        // В образце три частоты, значит три файла.
        var slices = GroupIterator.Enumerate(Sample, [Dimensions.FREQ]).ToList();

        Assert.Equal(3, slices.Count);
        Assert.Equal(3, GroupIterator.Count(Sample, [Dimensions.FREQ]));
    }

    [Fact]
    public void KeyCarriesCurrentAxisValues()
    {
        var slices = GroupIterator.Enumerate(Sample, [Dimensions.FREQ]).ToList();

        Assert.Equal("FREQ=2725000000", slices[0].Key);
        Assert.Equal("FREQ=2825000000", slices[2].Key);
    }

    [Fact]
    public void LastAxisOfTheListChangesMostOften()
    {
        // Порядок задаёт шаблон: первая ось списка внешняя.
        var slices = GroupIterator.Enumerate(Sample, [Dimensions.FREQ, Dimensions.POS]).Take(4).ToList();

        Assert.Equal("FREQ=2725000000;POS=-1.3", slices[0].Key);
        Assert.Equal("FREQ=2725000000;POS=-1.25", slices[1].Key);
    }

    [Fact]
    public void AnyNumberOfGroupAxesIsSupported()
    {
        Assert.Equal(1, GroupIterator.Count(Sample, []));
        Assert.Equal(3, GroupIterator.Count(Sample, [Dimensions.FREQ]));
        Assert.Equal(3 * 53, GroupIterator.Count(Sample, [Dimensions.FREQ, Dimensions.POS]));
        Assert.Equal(3 * 53 * 61, GroupIterator.Count(Sample, [Dimensions.FREQ, Dimensions.POS, Dimensions.DATA]));
    }

    [Fact]
    public void AxisAbsentFromTheFileIsRejected()
    {
        Assert.Throws<MatrixFormatException>(
            () => GroupIterator.Enumerate(Sample, [Dimensions.BEAM]).ToList());
    }

    [Fact]
    public void GroupAxisIsFixedInsideTheSlice()
    {
        var slice = GroupIterator.Enumerate(Sample, [Dimensions.FREQ]).ElementAt(1);

        Assert.Equal(1, slice.IndexOf(Dimensions.FREQ));
        Assert.Equal(-1, slice.IndexOf(Dimensions.POS));
        Assert.Equal("2775000000", slice.ValueOf(Dimensions.FREQ));
        Assert.Null(slice.ValueOf(Dimensions.POS));
    }
}
