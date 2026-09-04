using System.Globalization;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Строка отчёта о сформированном файле (ТЗ п. 4.3.3).
/// </summary>
/// <param name="Id">Идентификатор задания, полученный ключом <c>/id</c>.</param>
/// <param name="Code">Код результата: 0 — ОК, 1 — не ОК.</param>
/// <param name="Key">Ключ результирующего файла: значения осей отбора данных.</param>
/// <param name="File">Имя результирующего файла.</param>
/// <param name="Message">Текст сообщения об ошибке, если код не нулевой.</param>
public readonly record struct ResultLine(string Id, int Code, string Key, string File, string Message)
{
    /// <summary>Успешно сформированный файл.</summary>
    public static ResultLine Success(string id, string key, string file) =>
        new(id, 0, key, file, string.Empty);

    /// <summary>Файл сформировать не удалось.</summary>
    public static ResultLine Failure(string id, string key, string file, string message) =>
        new(id, 1, key, file, message);

    /// <summary>
    /// Строка протокола: пять полей через табуляцию.
    /// </summary>
    /// <remarks>
    /// Табуляция внутри полей заменяется пробелом: её появление сдвинуло бы колонки
    /// и сломало разбор на стороне заказчика.
    /// </remarks>
    public string Format() => string.Join(
        '\t',
        Clean(Id),
        Code.ToString(CultureInfo.InvariantCulture),
        Clean(Key),
        Clean(File),
        Clean(Message));

    private static string Clean(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
