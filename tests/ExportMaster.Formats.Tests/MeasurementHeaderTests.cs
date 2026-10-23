using ExportMaster.Formats;

namespace ExportMaster.Formats.Tests;

/// <summary>
/// Блок данных заголовка файла измерения.
/// </summary>
/// <remarks>
/// Значения сверены с байтами образца от автора формата независимым разбором,
/// а не выводом самой программы.
/// </remarks>
public class MeasurementHeaderTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa_6.mtx");

    private static MeasurementHeader Sample => MatrixReader.Read(SamplePath).Header;

    [Fact]
    public void FixedBlockIsOneHundredAndFifteenBytes()
    {
        // Сумма размеров 35 полей таблицы ТЗ п. 4.2.1.6.
        Assert.Equal(115, MeasurementHeader.FixedBlockSize);
    }

    [Fact]
    public void EnumeratedFieldsAreRecognized()
    {
        var header = Sample;

        Assert.Equal(MeasurementKind.Cut, header.MeasurementType);
        Assert.Equal(CoordinateSystemKind.ElAz, header.CoordinateSystem);
        Assert.Equal(ChamberKind.FarField, header.ChamberType);
    }

    [Theory]
    [InlineData(HeaderField.MeasurementType, "Сечение")]
    [InlineData(HeaderField.CoordinateSystem, "ЭлАз")]
    [InlineData(HeaderField.ChamberType, "ДЗ")]
    public void EnumeratedFieldsReadAsLabels(HeaderField field, string label)
    {
        var value = Sample.Read(field);

        Assert.Equal(HeaderValueKind.Label, value.Kind);
        Assert.Equal(label, value.Text);
    }

    [Fact]
    public void StringBlockIsReadAndNumberedFromZero()
    {
        var header = Sample;

        Assert.Equal(["1", "1"], header.Strings);
        Assert.Equal(0, header.AuxType);
        Assert.Equal(1, header.AuxIdentifier);
    }

    [Theory]
    [InlineData(HeaderField.AuxType, "1")]
    [InlineData(HeaderField.AuxIdentifier, "1")]
    public void StringReferencesReadAsStrings(HeaderField field, string text)
    {
        var value = Sample.Read(field);

        Assert.Equal(HeaderValueKind.Text, value.Kind);
        Assert.Equal(text, value.Text);
    }

    [Theory]
    [InlineData(HeaderField.AuxPolarization)] // код 0
    [InlineData(HeaderField.ReferencePortBalancing)] // код 255
    [InlineData(HeaderField.ReferenceChanelBalancing)]
    [InlineData(HeaderField.ChanelStatic)]
    [InlineData(HeaderField.PolarizationStatic)]
    [InlineData(HeaderField.SW1)] // ссылка −1
    [InlineData(HeaderField.SW2)]
    [InlineData(HeaderField.SW3)]
    [InlineData(HeaderField.ZeroX)] // NaN
    [InlineData(HeaderField.ZeroEl)]
    public void UnsetFieldsAreReportedAsUnset(HeaderField field)
    {
        Assert.Equal(HeaderValueKind.Unset, Sample.Read(field).Kind);
    }

    [Fact]
    public void NumericFieldsMatchTheFileContent()
    {
        var header = Sample;

        // Ширина полосы и мощность не заданы: в файле на их месте NaN.
        Assert.True(double.IsNaN(header.IFBW));
        Assert.True(double.IsNaN(header.P));
        Assert.Equal(HeaderValueKind.Unset, header.Read(HeaderField.IFBW).Kind);
        Assert.Equal(HeaderValueKind.Unset, header.Read(HeaderField.P).Kind);
        Assert.Equal(0d, header.Distance);
        Assert.Equal(0d, header.EquivalentDistance);
        Assert.Equal(0d, header.MeasurementDistance);
    }

    [Theory]
    // Линейные оси дают метры, угловые — градусы.
    [InlineData(DataAxisKind.X, DistanceUnit.Meters)]
    [InlineData(DataAxisKind.Y, DistanceUnit.Meters)]
    [InlineData(DataAxisKind.Az, DistanceUnit.Degrees)]
    [InlineData(DataAxisKind.El, DistanceUnit.Degrees)]
    [InlineData(DataAxisKind.Theta, DistanceUnit.Degrees)]
    public void MeasurementDistanceTakesItsUnitFromTheDataAxis(DataAxisKind axis, DistanceUnit unit)
    {
        var header = new MeasurementHeader { DataAxisCode = (byte)axis, MeasurementDistance = 0.5 };

        Assert.Equal(unit, header.MeasurementDistanceUnit);
        Assert.Equal(unit, header.Read(HeaderField.MeasurementDistance).Unit);
    }

    [Fact]
    public void SampleCollectsDataAlongAnAngularAxis()
    {
        var header = Sample;

        Assert.Equal(DataAxisKind.Az, header.DataAxis);
        Assert.Equal(DistanceUnit.Degrees, header.MeasurementDistanceUnit);
    }

    [Fact]
    public void UnknownDataAxisLeavesTheDistanceWithoutAUnit()
    {
        // Кода вне перечисления ТЗ быть не должно; если он встретится, единицу
        // измерения приписывать не из чего.
        var header = new MeasurementHeader { DataAxisCode = 9, MeasurementDistance = 0.5 };

        Assert.Null(header.DataAxis);
        Assert.Equal(HeaderValueKind.UnknownCode, header.Read(HeaderField.DataAxis).Kind);
        Assert.Equal(DistanceUnit.None, header.MeasurementDistanceUnit);
    }

    [Fact]
    public void DatesAreSecondsSinceNineteenSeventy()
    {
        var value = Sample.Read(HeaderField.DateStart);

        Assert.Equal(HeaderValueKind.Moment, value.Kind);
        Assert.Equal(1_791_462_793d, value.Number);
    }

    [Fact]
    public void ZeroSyncModeMeansThereWasNoSynchronisation()
    {
        // ТЗ ред. 1.3: ноль в полях 17 и 18 означает «синхронизации не было либо
        // неизвестно». Отличить одно от другого файл не позволяет, поэтому поле
        // выводится как незаданное, а не как «Простой».
        var header = Sample;

        Assert.Equal(0, header.SyncModeTypeCode);
        Assert.Null(header.SyncModeType);
        Assert.Equal(HeaderValueKind.Unset, header.Read(HeaderField.SyncModeType).Kind);
        Assert.Equal(HeaderValueKind.Unset, header.Read(HeaderField.SyncMode).Kind);
    }

    [Theory]
    [InlineData(1, HeaderValueKind.Number)]
    [InlineData(7, HeaderValueKind.Number)]
    [InlineData(8, HeaderValueKind.UnknownCode)]
    public void SyncModeNumberIsLimitedToTheSpecifiedRange(byte code, HeaderValueKind kind)
    {
        // Номер режима синхронизации объявлен как 1…7.
        var header = new MeasurementHeader { SyncMode = code };

        Assert.Equal(kind, header.Read(HeaderField.SyncMode).Kind);
    }

    [Fact]
    public void UnknownEnumerationCodeIsReportedSeparatelyFromUnset()
    {
        // Чтобы сбитая раскладка не превратилась в «не задано» и не прошла незамеченной.
        var header = new MeasurementHeader { MeasurementTypeCode = 42 };
        var value = header.Read(HeaderField.MeasurementType);

        Assert.Equal(HeaderValueKind.UnknownCode, value.Kind);
        Assert.Equal(42d, value.Number);
    }

    [Fact]
    public void ReferenceOutsideTheStringBlockIsNotMistakenForText()
    {
        var header = new MeasurementHeader { AuxType = 7, Strings = ["одна"] };

        Assert.Equal(HeaderValueKind.UnknownCode, header.Read(HeaderField.AuxType).Kind);
    }

    /// <summary>Разбор имён полей.</summary>
    public class Names
    {
        [Fact]
        public void AllThirtyFiveFieldsAreListed()
        {
            Assert.Equal(35, HeaderFields.Names.Count);
        }

        [Theory]
        [InlineData("MeasurementType", HeaderField.MeasurementType)]
        [InlineData("IFBW", HeaderField.IFBW)]
        [InlineData("ZeroAz", HeaderField.ZeroAz)]
        [InlineData("ChanelStatic", HeaderField.ChanelStatic)]
        [InlineData("ChannelStatic", HeaderField.ChanelStatic)]
        [InlineData("ifbw", HeaderField.IFBW)]
        [InlineData("measurementtype", HeaderField.MeasurementType)]
        [InlineData("ZEROAZ", HeaderField.ZeroAz)]
        [InlineData("channelstatic", HeaderField.ChanelStatic)]
        public void NameFromTheSpecificationIsParsed(string name, HeaderField expected)
        {
            Assert.True(HeaderFields.TryParse(name, out var field));
            Assert.Equal(expected, field);
        }

        [Fact]
        public void UnknownNameIsRejected()
        {
            Assert.False(HeaderFields.TryParse("POW", out _));
            Assert.False(HeaderFields.TryParse(null, out _));
            Assert.False(HeaderFields.TryParse(string.Empty, out _));
        }

        [Theory]
        [InlineData("5")]
        [InlineData("MeasurementType, DateStart")]
        public void NumberOrListIsNotAFieldName(string name)
        {
            // Разбор перечисления принял бы и то и другое, подставив поле, которого
            // в шаблоне никто не называл.
            Assert.False(HeaderFields.TryParse(name, out _));
        }
    }
}
