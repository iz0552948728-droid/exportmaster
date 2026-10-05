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
    /// Написания, принимаемые наравне с именем из ТЗ.
    /// </summary>
    /// <remarks>
    /// В таблице ТЗ «channel» записано с одной «n». Имя из документа остаётся
    /// основным — по нему пишут шаблоны, — но правильное написание принимается
    /// тоже: иначе опечатка документа превращается в ошибку шаблона.
    /// </remarks>
    private static readonly Dictionary<string, HeaderField> Alternates = new(StringComparer.Ordinal)
    {
        ["ReferenceChannelBalancing"] = HeaderField.ReferenceChanelBalancing,
        ["ChannelStatic"] = HeaderField.ChanelStatic,
    };

    /// <summary>Все имена полей в порядке следования в файле.</summary>
    public static IReadOnlyList<string> Names { get; } =
        Enum.GetValues<HeaderField>().Select(field => field.ToString()).ToArray();

    /// <summary>Разбирает имя поля; регистр учитывается.</summary>
    public static bool TryParse(string? name, out HeaderField field)
    {
        field = default;

        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (Enum.TryParse(name, ignoreCase: false, out field) && Enum.IsDefined(field))
        {
            return true;
        }

        return Alternates.TryGetValue(name, out field);
    }

    /// <summary>
    /// Имя поля, отличающееся от переданного только регистром, — чтобы в сообщении
    /// об ошибке подсказать написание, а не выкладывать список из 35 имён.
    /// </summary>
    public static string? Suggest(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var candidate in Names)
        {
            if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        foreach (var candidate in Alternates.Keys)
        {
            if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }
}
