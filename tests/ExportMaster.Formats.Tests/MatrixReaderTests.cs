using ExportMaster.Core;
using ExportMaster.Formats;

namespace ExportMaster.Formats.Tests;

/// <summary>
/// Разбор файла многомерной матрицы.
/// </summary>
/// <remarks>
/// Проверки идут по образцу от автора формата: выдуманный файл подтвердил бы только
/// то, что читалка согласована сама с собой.
/// </remarks>
public class MatrixReaderTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa.mtx");

    private static MatrixFile Sample => MatrixReader.Read(SamplePath);

    [Fact]
    public void SampleFileIsRead()
    {
        Assert.Equal(DataNature.Complex, Sample.Nature);
        Assert.Equal(3, Sample.Axes.Count);
    }

    [Theory]
    [InlineData(0, Dimensions.FREQ, 3)]
    [InlineData(1, Dimensions.POS, 53)]
    [InlineData(2, Dimensions.DATA, 61)]
    public void AxesMatchTheDescriptors(int index, Dimensions type, int length)
    {
        var axis = Sample.Axes[index];

        Assert.Equal(type, axis.Type);
        Assert.Equal(length, axis.Length);
    }

    [Fact]
    public void RangeAxisLengthIsComputedFromStartEndAndStep()
    {
        // В файле у такой оси ValueCount равен трём — это число хранимых значений,
        // а не число точек: точек от -1.3 до 1.3 с шагом 0.05 получается 53.
        var axis = Sample.Axes[1];

        Assert.Equal(AxisValueSource.Range, axis.Source);
        Assert.Equal(53, axis.Length);
        Assert.Equal(-1.3, axis.NumberAt(0), 10);
        Assert.Equal(1.3, axis.NumberAt(52), 10);
    }

    [Fact]
    public void ListAxisKeepsItsValues()
    {
        var axis = Sample.Axes[0];

        Assert.Equal(AxisValueSource.List, axis.Source);
        Assert.Equal(2_725_000_000d, axis.NumberAt(0));
        Assert.Equal(2_775_000_000d, axis.NumberAt(1));
        Assert.Equal(2_825_000_000d, axis.NumberAt(2));
    }

    [Fact]
    public void PointCountIsProductOfAxisLengths()
    {
        Assert.Equal(3 * 53 * 61, Sample.PointCount);
    }

    [Fact]
    public void ComplexDataTakesTwoValuesPerPoint()
    {
        Assert.Equal(2, Sample.ValuesPerPoint);
    }

    [Fact]
    public void FirstPointMatchesTheFileContent()
    {
        // Значение сверено с байтами файла напрямую.
        var value = Sample.ValueAt([0, 0, 0]);

        Assert.Equal(0.000664, value.Real, 6);
        Assert.Equal(0.000197, value.Imaginary, 6);
    }

    [Fact]
    public void LastDescriptorIsTheAxisInsideTheArray()
    {
        // Соседние точки по последней оси лежат в файле подряд (ТЗ п. 4.2.1.4),
        // поэтому переход по ней сдвигает смещение на одну точку.
        var matrix = Sample;

        var first = matrix.ValueAt([0, 0, 0]);
        var alongData = matrix.ValueAt([0, 0, 1]);

        Assert.NotEqual(first, alongData);
        Assert.Equal(0.000347, alongData.Real, 6);
    }

    [Fact]
    public void AxisIsFoundByType()
    {
        Assert.Equal(0, Sample.IndexOfAxis(Dimensions.FREQ));
        Assert.Equal(2, Sample.IndexOfAxis(Dimensions.DATA));
        Assert.Equal(-1, Sample.IndexOfAxis(Dimensions.BEAM));
    }

    [Fact]
    public void IndexOutsideAxisIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample.ValueAt([0, 0, 61]));
    }

    [Fact]
    public void WrongNumberOfIndicesIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Sample.ValueAt([0, 0]));
    }

    /// <summary>Испорченные файлы.</summary>
    public class Malformed
    {
        [Fact]
        public void ForeignSignatureIsRejected()
        {
            var content = File.ReadAllBytes(SamplePath);
            content[0] = 2;

            var error = Assert.Throws<MatrixFormatException>(() => MatrixReader.Read(content));
            Assert.Contains("игнатур", error.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void TruncatedFileIsRejected()
        {
            var content = File.ReadAllBytes(SamplePath)[..200];

            Assert.Throws<MatrixFormatException>(() => MatrixReader.Read(content));
        }

        [Fact]
        public void UnknownAxisTypeIsRejected()
        {
            var content = File.ReadAllBytes(SamplePath);
            content[14] = 9; // тип значения первого описателя

            Assert.Throws<MatrixFormatException>(() => MatrixReader.Read(content));
        }

        [Fact]
        public void MetadataSizeThatDoesNotMatchIsRejected()
        {
            // Размер отсчитывается от начала файла; расхождение означает,
            // что раскладка понята неверно, и продолжать разбор нельзя.
            var content = File.ReadAllBytes(SamplePath);
            content[4] = 100;

            Assert.Throws<MatrixFormatException>(() => MatrixReader.Read(content));
        }
    }
}
