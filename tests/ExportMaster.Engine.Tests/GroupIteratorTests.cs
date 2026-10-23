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
        MatrixReader.Read(Path.Combine(AppContext.BaseDirectory, "samples", "aaa_6.mtx"));

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

        Assert.Equal("FREQ=20.000GHz", slices[0].Key(Formatter));
        Assert.Equal("FREQ=40.000GHz", slices[2].Key(Formatter));
    }

    [Fact]
    public void LastAxisOfTheListChangesMostOften()
    {
        // Порядок задаёт шаблон: первая ось списка внешняя.
        var slices = GroupIterator.Enumerate(Sample, [Dimensions.FREQ, Dimensions.BEAM]).Take(4).ToList();

        Assert.Equal("FREQ=20.000GHz;BEAM=Луч 0", slices[0].Key(Formatter));
        Assert.Equal("FREQ=20.000GHz;BEAM=Луч 1", slices[1].Key(Formatter));
    }

    [Fact]
    public void AnyNumberOfGroupAxesIsSupported()
    {
        Assert.Equal(1, GroupIterator.Count(Sample, []));
        Assert.Equal(3, GroupIterator.Count(Sample, [Dimensions.FREQ]));
        Assert.Equal(3 * 2, GroupIterator.Count(Sample, [Dimensions.FREQ, Dimensions.BEAM]));
        Assert.Equal(3 * 2 * 1801, GroupIterator.Count(Sample, [Dimensions.FREQ, Dimensions.BEAM, Dimensions.DATA]));
    }

    [Fact]
    public void AxisAbsentFromTheFileIsRejected()
    {
        Assert.Throws<MatrixFormatException>(
            () => GroupIterator.Enumerate(Sample, [Dimensions.POS]).ToList());
    }

    [Fact]
    public void GroupAxisIsFixedInsideTheSlice()
    {
        var slice = GroupIterator.Enumerate(Sample, [Dimensions.FREQ]).ElementAt(1);

        Assert.Equal(1, slice.IndexOf(Dimensions.FREQ));
        Assert.Equal(-1, slice.IndexOf(Dimensions.POS));
        Assert.Equal("30.000GHz", Formatter.Format(slice.DescriptorOf(Dimensions.FREQ)!.Value.Descriptor, 1));
        Assert.Null(slice.DescriptorOf(Dimensions.POS));
    }
}
