using ExportMaster.Core;
using ExportMaster.Engine.Data;
using ExportMaster.Engine.Jobs;
using ExportMaster.Engine.Rendering;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Tests;

/// <summary>Разбиение матрицы по осям.</summary>
public class GroupIteratorTests
{
    private static AxisValueFormatter Formatter => new(new OutputSettings());

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

        Assert.Equal("FREQ=2.725GHz", slices[0].Key(Formatter));
        Assert.Equal("FREQ=2.825GHz", slices[2].Key(Formatter));
    }

    [Fact]
    public void LastAxisOfTheListChangesMostOften()
    {
        // Порядок задаёт шаблон: первая ось списка внешняя.
        var slices = GroupIterator.Enumerate(Sample, [Dimensions.FREQ, Dimensions.POS]).Take(4).ToList();

        Assert.Equal("FREQ=2.725GHz;POS=-1.3", slices[0].Key(Formatter));
        Assert.Equal("FREQ=2.725GHz;POS=-1.25", slices[1].Key(Formatter));
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
        Assert.Equal("2.775GHz", Formatter.Format(slice.DescriptorOf(Dimensions.FREQ)!.Value.Descriptor, 1));
        Assert.Null(slice.DescriptorOf(Dimensions.POS));
    }
}
