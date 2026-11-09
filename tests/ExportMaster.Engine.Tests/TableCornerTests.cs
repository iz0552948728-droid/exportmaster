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
/// Образец ред. 1.5 от автора формата: цилиндрический скан на две поляризации.
/// Единицы объявлены в описателях — безразмерная поляризация, герцы, метры, градусы.
/// </remarks>
public class TableCornerTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa-1.5.mtx");

    private static RenderContext Context => new(new OutputSettings());

    private static MatrixValueResolver Resolver => new(MatrixReader.Read(SamplePath));

    [Fact]
    public void UnitsOfBothAxesAreSeparatedBySlash()
    {
        // Движение по цилиндру в метрах, сбор данных по углу — в градусах.
        Assert.Equal("м/град", Resolver.TableCorner(Dimensions.POS, Dimensions.DATA, Context));
        Assert.Equal("град/м", Resolver.TableCorner(Dimensions.DATA, Dimensions.POS, Context));
    }

    [Fact]
    public void AxisWithoutAUnitLeavesItsSideEmpty()
    {
        // У поляризации единица объявлена безразмерной.
        Assert.Equal("/град", Resolver.TableCorner(Dimensions.POL1, Dimensions.DATA, Context));
        Assert.Equal("м/", Resolver.TableCorner(Dimensions.POS, Dimensions.POL1, Context));
    }

    [Fact]
    public void FrequencyIsLabelledByItsPrefixAndNotByTheDeclaredHertz()
    {
        // В описателе объявлены герцы, а значения приведены к гигагерцам —
        // подпись должна совпадать с тем, что написано в заголовках.
        Assert.Equal("ГГц/град", Resolver.TableCorner(Dimensions.FREQ, Dimensions.DATA, Context));
    }

    [Fact]
    public void CornerIsEmptyWhenNeitherAxisHasAUnit()
    {
        // Одинокая черта ничего не сообщает.
        Assert.Equal(string.Empty, Resolver.TableCorner(Dimensions.POL1, Dimensions.POL1, Context));
    }
}
