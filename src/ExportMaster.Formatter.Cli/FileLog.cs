using System.Globalization;
using System.Text;
using ExportMaster.Template.Diagnostics;

namespace ExportMaster.Formatter.Cli;

/// <summary>
/// Журнал работы: папка <c>LOGS</c> рядом с исполняемым файлом,
/// хранение за последние 10 дней (ТЗ п. 4.1.4).
/// </summary>
public sealed class FileLog
{
    private const int RetentionDays = 10;

    private readonly string? _path;

    private FileLog(string? path) => _path = path;

    /// <summary>
    /// Открывает журнал и удаляет записи старше срока хранения.
    /// </summary>
    /// <remarks>
    /// Невозможность вести журнал не должна останавливать задание: программа
    /// сообщает об этом в поток ошибок и работает дальше.
    /// </remarks>
    public static FileLog Open(TextWriter errorOutput)
    {
        ArgumentNullException.ThrowIfNull(errorOutput);

        try
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "LOGS");
            Directory.CreateDirectory(directory);

            RemoveExpired(directory);

            var name = $"exportmaster-{DateTime.Now:yyyy-MM-dd}.log";
            return new FileLog(Path.Combine(directory, name));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errorOutput.WriteLine($"Журнал недоступен: {exception.Message}");
            return new FileLog(path: null);
        }
    }

    public void Write(string message)
    {
        if (_path is null)
        {
            return;
        }

        var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        try
        {
            File.AppendAllText(_path, $"{stamp}  {message}{Environment.NewLine}", Encoding.UTF8);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Запись в журнал не должна ронять задание.
        }
    }

    /// <summary>Пишет замечания к шаблону с указанием файла, строки и колонки.</summary>
    public void WriteDiagnostics(string templatePath, IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (var diagnostic in diagnostics)
        {
            var severity = diagnostic.Severity == DiagnosticSeverity.Error ? "ошибка" : "предупреждение";
            Write($"{templatePath}{diagnostic.Span}: {severity}: {diagnostic.Message}");
        }
    }

    private static void RemoveExpired(string directory)
    {
        var threshold = DateTime.Now.AddDays(-RetentionDays);

        foreach (var file in Directory.EnumerateFiles(directory, "exportmaster-*.log"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < threshold)
                {
                    File.Delete(file);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Занятый или недоступный файл журнала пропускаем.
            }
        }
    }
}
