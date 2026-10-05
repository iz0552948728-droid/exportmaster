namespace ExportMaster.Formats;

/// <summary>Что именно лежит в поле заголовка.</summary>
public enum HeaderValueKind : byte
{
    /// <summary>Значение не задано: 255, −1 или NaN — смотря по полю.</summary>
    Unset = 0,

    /// <summary>Подпись значения перечислимого поля.</summary>
    Label = 1,

    /// <summary>Строка из блока строк заголовка.</summary>
    Text = 2,

    /// <summary>Число.</summary>
    Number = 3,

    /// <summary>Момент времени.</summary>
    Moment = 4,

    /// <summary>Код, которого нет в перечислении ТЗ, либо ссылка в никуда.</summary>
    UnknownCode = 5,
}

/// <summary>
/// Значение поля заголовка в том виде, в каком его хранит файл.
/// </summary>
/// <remarks>
/// Намеренно не текст: решение о том, как число, момент времени или признак
/// «не задано» выглядят в выходном файле, принимает не читалка формата, а
/// форматирующая сторона — см. <c>HeaderFieldFormatter</c>.
/// </remarks>
public readonly record struct HeaderValue
{
    private HeaderValue(HeaderValueKind kind, double number, string? text, DistanceUnit unit)
    {
        Kind = kind;
        Number = number;
        Text = text;
        Unit = unit;
    }

    public HeaderValueKind Kind { get; }

    /// <summary>Число — для <see cref="HeaderValueKind.Number"/>, <see cref="HeaderValueKind.Moment"/>
    /// (секунды от 1.1.1970) и <see cref="HeaderValueKind.UnknownCode"/>.</summary>
    public double Number { get; }

    /// <summary>Текст — для <see cref="HeaderValueKind.Label"/> и <see cref="HeaderValueKind.Text"/>.</summary>
    public string? Text { get; }

    /// <summary>Единица измерения, если она у поля есть.</summary>
    public DistanceUnit Unit { get; }

    public static HeaderValue Unset() => new(HeaderValueKind.Unset, double.NaN, null, DistanceUnit.None);

    public static HeaderValue Label(string text) => new(HeaderValueKind.Label, double.NaN, text, DistanceUnit.None);

    public static HeaderValue FromText(string text) => new(HeaderValueKind.Text, double.NaN, text, DistanceUnit.None);

    public static HeaderValue FromNumber(double value, DistanceUnit unit = DistanceUnit.None) =>
        new(HeaderValueKind.Number, value, null, unit);

    public static HeaderValue Moment(double unixSeconds) =>
        new(HeaderValueKind.Moment, unixSeconds, null, DistanceUnit.None);

    public static HeaderValue UnknownCode(double code) =>
        new(HeaderValueKind.UnknownCode, code, null, DistanceUnit.None);
}
