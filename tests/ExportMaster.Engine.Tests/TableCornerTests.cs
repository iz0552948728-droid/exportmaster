using ExportMaster.Core;
using ExportMaster.Engine.Data;
using ExportMaster.Engine.Jobs;
using ExportMaster.Engine.Rendering;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Tests;

/// <summary>
/// Угловая ячейка таблицы: единицы колонок и строк через косую черту.
/// </summary>
/// <remarks>
/// Образца ред. 1.5 ещё нет, поэтому файл автора формата пересобран под новую
/// раскладку: в каждый описатель вставлен байт единицы измерения.
/// </remarks>
public class TableCornerTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa_6.mtx");

    private static RenderContext Context => new(new OutputSettings());

    [Fact]
    public void UnitsOfBothAxesAreSeparatedBySlash()
    {
        // Луч объявлен в метрах — как ось движения у цилиндрического скана,
        // позиция данных в градусах.
        var resolver = Resolver([MeasureUnit.Meters, MeasureUnit.Hertz, MeasureUnit.Degrees]);

        Assert.Equal("м/град", resolver.TableCorner(Dimensions.BEAM, Dimensions.DATA, Context));
        Assert.Equal("град/м", resolver.TableCorner(Dimensions.DATA, Dimensions.BEAM, Context));
    }

    [Fact]
    public void AxisWithoutAUnitLeavesItsSideEmpty()
    {
        var resolver = Resolver([MeasureUnit.None, MeasureUnit.Hertz, MeasureUnit.Degrees]);

        Assert.Equal("/град", resolver.TableCorner(Dimensions.BEAM, Dimensions.DATA, Context));
    }

    [Fact]
    public void FrequencyIsLabelledByItsPrefixAndNotByTheDeclaredHertz()
    {
        // В описателе объявлены герцы, а значения приведены к гигагерцам —
        // подпись должна совпадать с тем, что написано в заголовках.
        var resolver = Resolver([MeasureUnit.Meters, MeasureUnit.Hertz, MeasureUnit.Degrees]);

        Assert.Equal("ГГц/град", resolver.TableCorner(Dimensions.FREQ, Dimensions.DATA, Context));
    }

    [Fact]
    public void CornerIsEmptyWhenNeitherAxisHasAUnit()
    {
        // Одинокая черта ничего не сообщает.
        var resolver = Resolver([MeasureUnit.None, MeasureUnit.Hertz, MeasureUnit.None]);

        Assert.Equal(string.Empty, resolver.TableCorner(Dimensions.BEAM, Dimensions.DATA, Context));
        Assert.Equal(string.Empty, resolver.TableCorner(Dimensions.DATA, Dimensions.BEAM, Context));
    }

    private static MatrixValueResolver Resolver(MeasureUnit[] units) =>
        new(MatrixReader.Read(WithUnits(units)));

    /// <summary>Пересобирает образец под ред. 1.5, проставляя осям заданные единицы.</summary>
    private static byte[] WithUnits(MeasureUnit[] units)
    {
        var source = File.ReadAllBytes(SamplePath);
        const int descriptorsStart = 137;
        var metadataSize = (int)BitConverter.ToUInt32(source, 4);

        var result = new List<byte>(source.Length + units.Length);
        result.AddRange(source[..descriptorsStart]);

        var position = descriptorsStart;

        foreach (var unit in units)
        {
            result.AddRange(source[position..(position + 5)]);
            result.Add((byte)unit);

            var valuesSize = (int)BitConverter.ToUInt32(source, position + 5);
            result.AddRange(source[(position + 5)..(position + 9 + valuesSize)]);
            position += 9 + valuesSize;
        }

        result.AddRange(source[position..]);

        var content = result.ToArray();
        BitConverter.GetBytes((uint)(metadataSize + units.Length)).CopyTo(content, 4);

        return content;
    }
}
