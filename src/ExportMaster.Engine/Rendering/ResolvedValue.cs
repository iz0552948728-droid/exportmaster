namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Значение, подставляемое вместо специального поля.
/// </summary>
/// <param name="Text">Текст для вставки в выходной файл.</param>
/// <param name="Number">
/// Числовое значение, если оно известно. Нужно полю <c>FORMAT</c>: форматировать
/// можно только число, а заглушка-описание числом не является.
/// </param>
public readonly record struct ResolvedValue(string Text, double? Number = null)
{
    /// <summary>Значение без числа — только текст.</summary>
    public static ResolvedValue FromText(string text) => new(text);

    /// <summary>Числовое значение с уже подготовленным представлением.</summary>
    public static ResolvedValue FromNumber(double number, string text) => new(text, number);
}
