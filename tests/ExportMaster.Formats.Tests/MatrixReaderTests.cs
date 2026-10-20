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
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa_5.mtx");

    /// <summary>Смещение первого описателя структуры в образце.</summary>
    private const int FirstDescriptor = 137;

    private static MatrixFile Sample => MatrixReader.Read(SamplePath);

    [Fact]
    public void SampleFileIsRead()
    {
        Assert.Equal(DataNature.Complex, Sample.Nature);
        Assert.Equal(3, Sample.Axes.Count);
    }

    [Theory]
    [InlineData(0, Dimensions.BEAM, 2)]
    [InlineData(1, Dimensions.FREQ, 3)]
    [InlineData(2, Dimensions.DATA, 1801)]
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
        // а не число точек: точек от -90 до 90 с шагом 0.1 получается 1801.
        var axis = Sample.Axes[2];

        Assert.Equal(AxisValueSource.Range, axis.Source);
        Assert.Equal(1801, axis.Length);
        Assert.Equal(-90, axis.NumberAt(0), 10);
        Assert.Equal(90, axis.NumberAt(1800), 10);
    }

    [Fact]
    public void RangeAxisDoesNotLeakAccumulatedError()
    {
        // -90 + 0.1*1 в двоичной арифметике не точное число; двенадцати значащих
        // цифр хватает, чтобы наружу вышло -89.9, а не -89.90000000000001.
        Assert.Equal("-89.9", Sample.Axes[2].TextAt(1));
    }

    [Fact]
    public void StringAxisKeepsItsValues()
    {
        // Имена лучей: ось задана списком строк, а не числами.
        var axis = Sample.Axes[0];

        Assert.Equal(AxisValueSource.List, axis.Source);
        Assert.Equal(AxisValueKind.Strings, axis.ValueKind);
        Assert.Equal("Луч 0", axis.TextAt(0));
        Assert.Equal("Луч 1", axis.TextAt(1));
    }

    [Fact]
    public void FrequencyRangeIsReadInHertz()
    {
        var axis = Sample.Axes[1];

        Assert.Equal(20_000_000_000d, axis.NumberAt(0));
        Assert.Equal(30_000_000_000d, axis.NumberAt(1));
        Assert.Equal(40_000_000_000d, axis.NumberAt(2));
    }

    [Fact]
    public void PointCountIsProductOfAxisLengths()
    {
        Assert.Equal(2 * 3 * 1801, Sample.PointCount);
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

        Assert.Equal(2.36844e-05, value.Real, 10);
        Assert.Equal(4.19301e-05, value.Imaginary, 10);
    }

    [Fact]
    public void LastDescriptorIsTheAxisInsideTheArray()
    {
        // Соседние точки по последней оси лежат в файле подряд (ТЗ п. 4.2.1.8),
        // поэтому переход по ней сдвигает смещение на одну точку.
        var matrix = Sample;

        var first = matrix.ValueAt([0, 0, 0]);
        var alongData = matrix.ValueAt([0, 0, 1]);

        Assert.NotEqual(first, alongData);
        Assert.Equal(-1.92659e-06, alongData.Real, 10);
    }

    [Fact]
    public void AxisIsFoundByType()
    {
        Assert.Equal(0, Sample.IndexOfAxis(Dimensions.BEAM));
        Assert.Equal(1, Sample.IndexOfAxis(Dimensions.FREQ));
        Assert.Equal(2, Sample.IndexOfAxis(Dimensions.DATA));
        Assert.Equal(-1, Sample.IndexOfAxis(Dimensions.POS));
    }

    [Fact]
    public void IndexOutsideAxisIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample.ValueAt([0, 0, 1801]));
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
            content[FirstDescriptor] = 9; // тип значения первого описателя

            Assert.Throws<MatrixFormatException>(() => MatrixReader.Read(content));
        }

        [Fact]
        public void ValuesSizeThatDoesNotMatchIsRejected()
        {
            // Размер массива значений объявлен в самом описателе; расхождение означает,
            // что состав значений понят неверно, и следующий описатель поедет.
            var content = File.ReadAllBytes(SamplePath);
            content[FirstDescriptor + 5] = 19; // размер значений первого описателя, было 18

            var error = Assert.Throws<MatrixFormatException>(() => MatrixReader.Read(content));
            Assert.Contains("размер", error.Message, StringComparison.Ordinal);
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
