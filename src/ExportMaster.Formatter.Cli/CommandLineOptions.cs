using ExportMaster.Engine.Rendering;

namespace ExportMaster.Formatter.Cli;

/// <summary>
/// Параметры запуска модуля форматирования (ТЗ п. 4.3.4).
/// </summary>
public sealed class CommandLineOptions
{
    public string TemplatePath { get; set; } = string.Empty;

    public string TargetDirectory { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public Dictionary<string, string> Sources { get; } = new(StringComparer.Ordinal);

    public string? Key { get; set; }

    public bool Overwrite { get; set; } = true;

    public StubStyle StubStyle { get; set; } = StubStyle.Descriptive;

    /// <summary>Справка по ключам.</summary>
    public static string Usage => """
        МАСТЕР ЭКСПОРТА — модуль форматирования

          /source <алиас>:<файл>   файл данных под заданным алиасом, можно указывать многократно
          /md <файл>               файл шаблона
          /target <путь>           каталог для результата
          /id <идентификатор>      идентификатор задания
          /key <ключ>              сформировать файл для конкретного значения ключа
          /overwrite yes|no        перезаписывать существующий файл, по умолчанию yes

        Отладочные ключи, вне ТЗ:

          /stub descriptive|sample вид заглушек вместо значений, по умолчанию descriptive

        Каждый ключ принимается и в длинной форме: --source, --md и так далее.
        """;
}
