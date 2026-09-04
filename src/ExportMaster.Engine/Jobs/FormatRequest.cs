using ExportMaster.Engine.Rendering;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Задание модулю форматирования.
/// </summary>
/// <remarks>
/// Один и тот же объект строят и режим командной строки, и диалоговый режим:
/// вся логика живёт в движке, оболочки остаются тонкими.
/// </remarks>
public sealed class FormatRequest
{
    /// <summary>Путь к файлу шаблона (ключ <c>/md</c>).</summary>
    public required string TemplatePath { get; init; }

    /// <summary>Каталог для результата (ключ <c>/target</c>).</summary>
    public required string TargetDirectory { get; init; }

    /// <summary>Идентификатор задания (ключ <c>/id</c>).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Файлы данных по алиасам (ключ <c>/source</c>).</summary>
    public IReadOnlyDictionary<string, string> Sources { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Ключ конкретного файла (ключ <c>/key</c>); на этом этапе не используется для отбора.</summary>
    public string? Key { get; init; }

    /// <summary>Перезаписывать существующий выходной файл (ключ <c>/overwrite</c>).</summary>
    public bool Overwrite { get; init; } = true;

    /// <summary>Вид подставляемых заглушек.</summary>
    public StubStyle StubStyle { get; init; } = StubStyle.Descriptive;
}
