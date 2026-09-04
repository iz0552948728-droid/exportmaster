using ExportMaster.Core;

namespace ExportMaster.Core.Tests;

public class DimensionsTests
{
    [Theory]
    [InlineData(Dimensions.FREQ, 1)]
    [InlineData(Dimensions.POL1, 2)]
    [InlineData(Dimensions.CHANNEL, 3)]
    [InlineData(Dimensions.BEAM, 4)]
    [InlineData(Dimensions.DATA, 5)]
    [InlineData(Dimensions.POS, 6)]
    [InlineData(Dimensions.SLIDER, 7)]
    [InlineData(Dimensions.POL2, 8)]
    public void ValueMatchesByteInDataFile(Dimensions dimension, byte expected)
    {
        Assert.Equal(expected, (byte)dimension);
    }

    [Fact]
    public void UnderlyingTypeIsSingleByte()
    {
        // Размер важен: тип значения занимает в файле данных ровно один байт.
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(Dimensions)));
    }

    [Fact]
    public void ZeroIsNotAValidAxis()
    {
        // Защита от возврата к нумерации с нуля: значения 0 в формате нет.
        Assert.False(Enum.IsDefined(typeof(Dimensions), (byte)0));
    }

    [Fact]
    public void AllEightAxesAreDefined()
    {
        Assert.Equal(8, Enum.GetValues<Dimensions>().Length);
    }
}
