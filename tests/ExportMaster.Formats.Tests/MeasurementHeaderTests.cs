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
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "samples", "aaa_3.mtx");

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

        Assert.Equal(MeasurementKind.PlanarNearField, header.MeasurementType);
        Assert.Equal(CoordinateSystemKind.XY, header.CoordinateSystem);
        Assert.Equal(ChamberKind.NearField, header.ChamberType);
        Assert.Equal(DataAxisKind.Y, header.DataAxis);
    }

    [Theory]
    [InlineData(HeaderField.MeasurementType, "БЗП")]
    [InlineData(HeaderField.CoordinateSystem, "XY")]
    [InlineData(HeaderField.ChamberType, "БЗ")]
    [InlineData(HeaderField.DataAxis, "Y")]
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

        Assert.Equal(["ТМАЗ 2-4", "0526492"], header.Strings);
        Assert.Equal(0, header.AuxType);
        Assert.Equal(1, header.AuxIdentifier);
    }

    [Theory]
    [InlineData(HeaderField.AuxType, "ТМАЗ 2-4")]
    [InlineData(HeaderField.AuxIdentifier, "0526492")]
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

        Assert.Equal(1000d, header.IFBW);
        Assert.Equal(13d, header.P);
        Assert.Equal(0d, header.Distance);
        Assert.Equal(0d, header.EquivalentDistance);
        Assert.Equal(0.022498, header.MeasurementDistance, 6);
    }

    [Fact]
    public void MeasurementDistanceTakesItsUnitFromTheDataAxis()
    {
        // Ось сбора данных Y линейная, поэтому дистанция в метрах.
        Assert.Equal(DistanceUnit.Meters, Sample.MeasurementDistanceUnit);
        Assert.Equal(DistanceUnit.Meters, Sample.Read(HeaderField.MeasurementDistance).Unit);
    }

    [Fact]
    public void DatesAreSecondsSinceNineteenSeventy()
    {
        var value = Sample.Read(HeaderField.DateStart);

        Assert.Equal(HeaderValueKind.Moment, value.Kind);
        Assert.Equal(1_788_793_811d, value.Number);
    }

    [Fact]
    public void ZeroSyncModeTypeReadsAsSimple()
    {
        // Ноля в этом поле быть не может; по решению автора ТЗ он означает «Простой».
        var header = Sample;

        Assert.Equal(0, header.SyncModeTypeCode);
        Assert.Equal(SyncModeKind.Simple, header.SyncModeType);
        Assert.Equal("Простой", header.Read(HeaderField.SyncModeType).Text);
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
        public void NameFromTheSpecificationIsParsed(string name, HeaderField expected)
        {
            Assert.True(HeaderFields.TryParse(name, out var field));
            Assert.Equal(expected, field);
        }

        [Fact]
        public void CaseIsSignificant()
        {
            Assert.False(HeaderFields.TryParse("measurementtype", out _));
            Assert.Equal("MeasurementType", HeaderFields.Suggest("measurementtype"));
        }

        [Fact]
        public void UnknownNameHasNoSuggestion()
        {
            Assert.False(HeaderFields.TryParse("POW", out _));
            Assert.Null(HeaderFields.Suggest("POW"));
        }
    }
}
