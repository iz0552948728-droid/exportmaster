namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Значение, подставляемое вместо специального поля.
/// </summary>
/// <param name="Text">Текст для вставки в выходной файл.</param>
/// <param name="Number">
/// Числовое значение, если оно известно. Нужно полю <c>FORMAT</c>: форматировать
/// можно только число, а заглушка-описание числом не является.
/// </param>
/// <param name="IsUnset">
/// Значения нет: в файле на его месте <c>NaN</c>, <c>255</c> или <c>−1</c>. Такое
/// значение не форматируется, а выводится словами (ТЗ п. 4.4.7), поэтому отличать
/// его от «не числа» приходится явно.
/// </param>
public readonly record struct ResolvedValue(string Text, double? Number = null, bool IsUnset = false)
{
    /// <summary>Значение без числа — только текст.</summary>
    public static ResolvedValue FromText(string text) => new(text);

    /// <summary>Числовое значение с уже подготовленным представлением.</summary>
    public static ResolvedValue FromNumber(double number, string text) => new(text, number);

    /// <summary>Значения нет; текст задаётся настройками выходного файла.</summary>
    public static ResolvedValue Unset(string text) => new(text, null, true);
}
