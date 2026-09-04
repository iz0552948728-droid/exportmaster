using System.Text;
using ExportMaster.Engine.Jobs;
using ExportMaster.Template.Diagnostics;

namespace ExportMaster.Formatter.Cli;

/// <summary>
/// Точка входа модуля форматирования в режиме командной строки.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        ResultWriter.ConfigureConsole();

        // CP1251 в .NET доступна только после регистрации набора кодовых страниц.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var log = FileLog.Open(Console.Error);

        if (args.Length == 0)
        {
            Console.Error.WriteLine(CommandLineOptions.Usage);
            return 2;
        }

        var parsed = CommandLineParser.Parse(args);

        if (!parsed.Success)
        {
            foreach (var error in parsed.Errors)
            {
                Console.Error.WriteLine(error);
                log.Write($"ошибка аргументов: {error}");
            }

            return 2;
        }

        var options = parsed.Options!;
        log.Write($"задание {options.Id}: шаблон {options.TemplatePath}, каталог {options.TargetDirectory}");

        var request = new FormatRequest
        {
            TemplatePath = options.TemplatePath,
            TargetDirectory = options.TargetDirectory,
            Id = options.Id,
            Sources = options.Sources,
            Key = options.Key,
            Overwrite = options.Overwrite,
            StubStyle = options.StubStyle,
        };

        var result = new FormattingService().Execute(request);

        log.WriteDiagnostics(options.TemplatePath, result.Diagnostics);

        // Стандартный вывод — только протокол ТЗ п. 4.3.3.
        new ResultWriter(Console.Out).Write(result.Lines);

        foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            Console.Error.WriteLine($"{options.TemplatePath}{diagnostic.Span}: {diagnostic.Message}");
        }

        log.Write($"задание {options.Id}: код возврата {result.ExitCode}");
        return result.ExitCode;
    }
}
