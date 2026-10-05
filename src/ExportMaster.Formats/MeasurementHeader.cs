namespace ExportMaster.Formats;

/// <summary>
/// Блок данных заголовка файла измерения (ТЗ п. 4.2.1.5—4.2.1.7).
/// </summary>
/// <remarks>
/// Поля хранятся так, как записаны в файле: признаки «не задано» (255, −1, NaN)
/// не заменяются и не прячутся, а ссылки на блок строк остаются номерами. Разбор
/// значения в пригодный для вывода вид выполняет <see cref="Read"/>.
/// </remarks>
public sealed class MeasurementHeader
{
    /// <summary>Размер блока постоянного размера (ТЗ п. 4.2.1.6).</summary>
    public const int FixedBlockSize = 115;

    /// <summary>Код вида измерения; 255 — неопределено.</summary>
    public byte MeasurementTypeCode { get; init; }

    /// <summary>Код системы координат; 255 — неопределено.</summary>
    public byte CoordinateSystemCode { get; init; }

    /// <summary>Код типа камеры; 255 — неопределено.</summary>
    public byte ChamberTypeCode { get; init; }

    /// <summary>Эквивалентное расстояние дальней зоны; значимо только для коллиматора.</summary>
    public double EquivalentDistance { get; init; }

    /// <summary>Начало измерения: секунды, прошедшие с 1.1.1970.</summary>
    public ulong DateStart { get; init; }

    /// <summary>Завершение измерения: секунды, прошедшие с 1.1.1970.</summary>
    public ulong DateEnd { get; init; }

    /// <summary>Код поляризации измерительной антенны; 0 — не задано.</summary>
    public byte AuxPolarizationCode { get; init; }

    /// <summary>Наименование типа измерительной антенны: номер строки, −1 — не задано.</summary>
    public int AuxType { get; init; }

    /// <summary>Серийник измерительной антенны: номер строки, −1 — не задано.</summary>
    public int AuxIdentifier { get; init; }

    /// <summary>Расстояние (для цилиндра/сферы).</summary>
    public double Distance { get; init; }

    /// <summary>Ширина полосы ПЧ.</summary>
    public double IFBW { get; init; }

    /// <summary>Мощность.</summary>
    public double P { get; init; }

    /// <summary>Код оси сбора данных.</summary>
    public byte DataAxisCode { get; init; }

    /// <summary>Дистанция, на которой собирались данные в каждой точке; 0 — пошаговый режим.</summary>
    public double MeasurementDistance { get; init; }

    /// <summary>Номер опорного порта балансировки; 255 — балансировки не было.</summary>
    public byte ReferencePortBalancing { get; init; }

    /// <summary>Номер опорного канала балансировки; 255 — балансировки не было.</summary>
    public byte ReferenceChanelBalancing { get; init; }

    /// <summary>Номер режима синхронизации.</summary>
    public byte SyncMode { get; init; }

    /// <summary>Код вида режима синхронизации.</summary>
    public byte SyncModeTypeCode { get; init; }

    /// <summary>Использованные задержки.</summary>
    public uint T1 { get; init; }

    public uint T2 { get; init; }

    public uint T3 { get; init; }

    public uint T4 { get; init; }

    public uint T5 { get; init; }

    /// <summary>Имя свича 1: номер строки, −1 — не задано.</summary>
    public int SW1 { get; init; }

    public int SW2 { get; init; }

    public int SW3 { get; init; }

    /// <summary>Статическое положение канала ключа; 255 — не было.</summary>
    public byte ChanelStatic { get; init; }

    /// <summary>Статическое положение канала поляризации; 255 — не было.</summary>
    public byte PolarizationStatic { get; init; }

    /// <summary>Положения обнуления по осям; NaN — обнуление не проводилось.</summary>
    public double ZeroX { get; init; }

    public double ZeroY { get; init; }

    public double ZeroZ { get; init; }

    public double ZeroP { get; init; }

    public double ZeroR { get; init; }

    public double ZeroAz { get; init; }

    public double ZeroEl { get; init; }

    /// <summary>Строки блока переменного размера; нумерация с нуля (ТЗ п. 4.2.1.7).</summary>
    public IReadOnlyList<string> Strings { get; init; } = [];

    /// <summary>Вид измерения, если код известен.</summary>
    public MeasurementKind? MeasurementType => Known<MeasurementKind>(MeasurementTypeCode);

    /// <summary>Система координат, если код известен.</summary>
    public CoordinateSystemKind? CoordinateSystem => Known<CoordinateSystemKind>(CoordinateSystemCode);

    /// <summary>Тип камеры, если код известен.</summary>
    public ChamberKind? ChamberType => Known<ChamberKind>(ChamberTypeCode);

    /// <summary>Поляризация измерительной антенны, если код известен.</summary>
    public AuxPolarizationKind? AuxPolarization => Known<AuxPolarizationKind>(AuxPolarizationCode);

    /// <summary>Ось сбора данных, если код известен.</summary>
    public DataAxisKind? DataAxis => Known<DataAxisKind>(DataAxisCode);

    /// <summary>
    /// Вид режима синхронизации, если код известен.
    /// </summary>
    /// <remarks>
    /// Ноля в этом поле быть не может, и в образце он означает незаполненное поле:
    /// по решению автора ТЗ ноль читается как «Простой».
    /// </remarks>
    public SyncModeKind? SyncModeType =>
        SyncModeTypeCode == 0 ? SyncModeKind.Simple : Known<SyncModeKind>(SyncModeTypeCode);

    /// <summary>
    /// Единица измерения поля <see cref="MeasurementDistance"/>: её задаёт ось сбора
    /// данных — угловые оси дают градусы, линейные метры.
    /// </summary>
    public DistanceUnit MeasurementDistanceUnit => DataAxis switch
    {
        DataAxisKind.X or DataAxisKind.Y => DistanceUnit.Meters,
        DataAxisKind.Az or DataAxisKind.El or DataAxisKind.P
            or DataAxisKind.Theta or DataAxisKind.Phi => DistanceUnit.Degrees,
        _ => DistanceUnit.None,
    };

    /// <summary>Значение поля по его имени.</summary>
    public HeaderValue Read(HeaderField field) => field switch
    {
        HeaderField.MeasurementType => Enumerated(
            MeasurementTypeCode, (byte)MeasurementKind.Undefined, MeasurementType, HeaderLabels.Of),
        HeaderField.CoordinateSystem => Enumerated(
            CoordinateSystemCode, (byte)CoordinateSystemKind.Undefined, CoordinateSystem, HeaderLabels.Of),
        HeaderField.ChamberType => Enumerated(
            ChamberTypeCode, (byte)ChamberKind.Undefined, ChamberType, HeaderLabels.Of),
        HeaderField.EquivalentDistance => Real(EquivalentDistance),
        HeaderField.DateStart => Moment(DateStart),
        HeaderField.DateEnd => Moment(DateEnd),
        HeaderField.AuxPolarization => Enumerated(
            AuxPolarizationCode, (byte)AuxPolarizationKind.Unset, AuxPolarization, HeaderLabels.Of),
        HeaderField.AuxType => StringRef(AuxType),
        HeaderField.AuxIdentifier => StringRef(AuxIdentifier),
        HeaderField.Distance => Real(Distance),
        HeaderField.IFBW => Real(IFBW),
        HeaderField.P => Real(P),
        HeaderField.DataAxis => Enumerated(DataAxisCode, unsetCode: null, DataAxis, HeaderLabels.Of),
        HeaderField.MeasurementDistance => Real(MeasurementDistance, MeasurementDistanceUnit),
        HeaderField.ReferencePortBalancing => Optional(ReferencePortBalancing),
        HeaderField.ReferenceChanelBalancing => Optional(ReferenceChanelBalancing),
        HeaderField.SyncMode => HeaderValue.FromNumber(SyncMode),
        HeaderField.SyncModeType => Enumerated(
            SyncModeTypeCode, unsetCode: null, SyncModeType, HeaderLabels.Of),
        HeaderField.T1 => HeaderValue.FromNumber(T1),
        HeaderField.T2 => HeaderValue.FromNumber(T2),
        HeaderField.T3 => HeaderValue.FromNumber(T3),
        HeaderField.T4 => HeaderValue.FromNumber(T4),
        HeaderField.T5 => HeaderValue.FromNumber(T5),
        HeaderField.SW1 => StringRef(SW1),
        HeaderField.SW2 => StringRef(SW2),
        HeaderField.SW3 => StringRef(SW3),
        HeaderField.ChanelStatic => Optional(ChanelStatic),
        HeaderField.PolarizationStatic => Optional(PolarizationStatic),
        HeaderField.ZeroX => Real(ZeroX),
        HeaderField.ZeroY => Real(ZeroY),
        HeaderField.ZeroZ => Real(ZeroZ),
        HeaderField.ZeroP => Real(ZeroP),
        HeaderField.ZeroR => Real(ZeroR),
        HeaderField.ZeroAz => Real(ZeroAz),
        HeaderField.ZeroEl => Real(ZeroEl),
        _ => HeaderValue.Unset(),
    };

    private static T? Known<T>(byte code)
        where T : struct, Enum =>
        Enum.IsDefined((T)(object)code) ? (T)(object)code : null;

    /// <summary>Перечислимое поле: подпись, признак «не задано» либо неизвестный код.</summary>
    private static HeaderValue Enumerated<T>(byte code, byte? unsetCode, T? value, Func<T, string> label)
        where T : struct, Enum
    {
        if (code == unsetCode)
        {
            return HeaderValue.Unset();
        }

        return value is { } known ? HeaderValue.Label(label(known)) : HeaderValue.UnknownCode(code);
    }

    /// <summary>Вещественное поле: NaN означает «не задано».</summary>
    private static HeaderValue Real(double value, DistanceUnit unit = DistanceUnit.None) =>
        double.IsNaN(value) ? HeaderValue.Unset() : HeaderValue.FromNumber(value, unit);

    /// <summary>Байтовое поле, у которого 255 означает «не было».</summary>
    private static HeaderValue Optional(byte value) =>
        value == byte.MaxValue ? HeaderValue.Unset() : HeaderValue.FromNumber(value);

    /// <summary>Момент времени; выходящее за пределы календаря значение — неизвестный код.</summary>
    private static HeaderValue Moment(ulong seconds) =>
        seconds > (ulong)DateTimeOffset.MaxValue.ToUnixTimeSeconds()
            ? HeaderValue.UnknownCode(seconds)
            : HeaderValue.Moment(seconds);

    /// <summary>Ссылка на блок строк: −1 означает «не задано».</summary>
    private HeaderValue StringRef(int index)
    {
        if (index < 0)
        {
            return HeaderValue.Unset();
        }

        return index < Strings.Count ? HeaderValue.FromText(Strings[index]) : HeaderValue.UnknownCode(index);
    }
}
