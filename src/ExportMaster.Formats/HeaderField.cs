namespace ExportMaster.Formats;

/// <summary>
/// Поля блока данных заголовка (ТЗ п. 4.2.1.6).
/// </summary>
/// <remarks>
/// Имена членов повторяют имена полей из таблицы ТЗ буква в букву: именно их
/// пишет автор шаблона в поле <c>FIELD</c>, и расхождение здесь означало бы
/// расхождение между документом и программой. Номер члена — номер поля в таблице,
/// он же порядок следования в файле.
/// </remarks>
public enum HeaderField
{
    MeasurementType = 1,
    CoordinateSystem = 2,
    ChamberType = 3,
    EquivalentDistance = 4,
    DateStart = 5,
    DateEnd = 6,
    AuxPolarization = 7,
    AuxType = 8,
    AuxIdentifier = 9,
    Distance = 10,
    IFBW = 11,
    P = 12,
    DataAxis = 13,
    MeasurementDistance = 14,
    ReferencePortBalancing = 15,
    ReferenceChanelBalancing = 16,
    SyncMode = 17,
    SyncModeType = 18,
    T1 = 19,
    T2 = 20,
    T3 = 21,
    T4 = 22,
    T5 = 23,
    SW1 = 24,
    SW2 = 25,
    SW3 = 26,
    ChanelStatic = 27,
    PolarizationStatic = 28,
    ZeroX = 29,
    ZeroY = 30,
    ZeroZ = 31,
    ZeroP = 32,
    ZeroR = 33,
    ZeroAz = 34,
    ZeroEl = 35,
}

/// <summary>Разбор имени поля заголовка.</summary>
public static class HeaderFields
{
    /// <summary>
    /// Имена полей и принимаемые написания.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Регистр не учитывается: <c>IFBW</c>, <c>ifbw</c> и <c>Ifbw</c> означают одно поле.
    /// </para>
    /// <para>
    /// В таблице ТЗ «channel» записано с одной «n». Имя из документа остаётся основным —
    /// по нему пишут шаблоны, — но правильное написание принимается тоже: иначе опечатка
    /// документа превращается в ошибку шаблона.
    /// </para>
    /// <para>
    /// Поиск идёт по словарю, а не через <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>:
    /// тот принял бы и число, и перечисление через запятую, а <c>{FIELD("5")}</c> обязано
    /// быть ошибкой, а не пятым полем таблицы.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, HeaderField> ByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ReferenceChannelBalancing"] = HeaderField.ReferenceChanelBalancing,
            ["ChannelStatic"] = HeaderField.ChanelStatic,
        };

    static HeaderFields()
    {
        foreach (var field in Enum.GetValues<HeaderField>())
        {
            ByName[field.ToString()] = field;
        }
    }

    /// <summary>Все имена полей в порядке следования в файле.</summary>
    public static IReadOnlyList<string> Names { get; } =
        Enum.GetValues<HeaderField>().Select(field => field.ToString()).ToArray();

    /// <summary>Разбирает имя поля; регистр не учитывается.</summary>
    public static bool TryParse(string? name, out HeaderField field)
    {
        field = default;
        return name is not null && ByName.TryGetValue(name, out field);
    }
}
