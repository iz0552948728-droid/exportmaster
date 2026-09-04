using ExportMaster.Engine.Rendering;

namespace ExportMaster.Formatter.Cli;

/// <summary>
/// Разбирает командную строку.
/// </summary>
/// <remarks>
/// Ключи записываются со слэшем, как задано ТЗ п. 4.3.4, и дополнительно принимаются
/// в длинной форме: п. 4.9 допускает Linux, где <c>/source</c> неотличим
/// от абсолютного пути.
/// </remarks>
public static class CommandLineParser
{
    /// <summary>Итог разбора: параметры либо перечень ошибок.</summary>
    public sealed record Result(CommandLineOptions? Options, IReadOnlyList<string> Errors)
    {
        public bool Success => Options is not null && Errors.Count == 0;
    }

    public static Result Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var options = new CommandLineOptions();
        var errors = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var key = Normalize(args[index]);

            if (key is null)
            {
                errors.Add($"Не опознан аргумент '{args[index]}': ожидается ключ вида /имя.");
                continue;
            }

            switch (key)
            {
                case "source":
                    AddSource(NextValue(args, ref index, key, errors), options, errors);
                    break;

                case "md":
                    options.TemplatePath = NextValue(args, ref index, key, errors) ?? options.TemplatePath;
                    break;

                case "target":
                    options.TargetDirectory = NextValue(args, ref index, key, errors) ?? options.TargetDirectory;
                    break;

                case "id":
                    options.Id = NextValue(args, ref index, key, errors) ?? options.Id;
                    break;

                case "key":
                    options.Key = NextValue(args, ref index, key, errors) ?? options.Key;
                    break;

                case "overwrite":
                    ReadOverwrite(NextValue(args, ref index, key, errors), options, errors);
                    break;

                case "stub":
                    ReadStubStyle(NextValue(args, ref index, key, errors), options, errors);
                    break;

                default:
                    errors.Add($"Неизвестный ключ '/{key}'.");
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(options.TemplatePath))
        {
            errors.Add("Не задан файл шаблона: ключ /md обязателен.");
        }
        else if (!File.Exists(options.TemplatePath))
        {
            errors.Add($"Файл шаблона не найден: {options.TemplatePath}");
        }

        if (string.IsNullOrWhiteSpace(options.TargetDirectory))
        {
            errors.Add("Не задан каталог результата: ключ /target обязателен.");
        }

        return new Result(errors.Count == 0 ? options : null, errors);
    }

    /// <summary>Приводит <c>/source</c>, <c>--source</c> и <c>-source</c> к имени ключа.</summary>
    private static string? Normalize(string argument)
    {
        if (argument.StartsWith("--", StringComparison.Ordinal))
        {
            return argument[2..].ToLowerInvariant();
        }

        if (argument.StartsWith('/') || argument.StartsWith('-'))
        {
            return argument[1..].ToLowerInvariant();
        }

        return null;
    }

    private static string? NextValue(string[] args, ref int index, string key, List<string> errors)
    {
        if (index + 1 >= args.Length)
        {
            errors.Add($"Ключ /{key} требует значения.");
            return null;
        }

        index++;
        return args[index];
    }

    private static void AddSource(string? value, CommandLineOptions options, List<string> errors)
    {
        if (value is null)
        {
            return;
        }

        // Разделяем по первому двоеточию: путь в Windows содержит своё
        // (например DATA:C:\data\matrix.bin).
        var separator = value.IndexOf(':', StringComparison.Ordinal);

        if (separator <= 0 || separator == value.Length - 1)
        {
            errors.Add($"Ключ /source ожидает запись вида алиас:файл, а получено '{value}'.");
            return;
        }

        var alias = value[..separator];
        var path = value[(separator + 1)..];

        if (!options.Sources.TryAdd(alias, path))
        {
            errors.Add($"Алиас '{alias}' указан повторно.");
            return;
        }

        if (!File.Exists(path))
        {
            errors.Add($"Файл данных не найден: {path}");
        }
    }

    private static void ReadOverwrite(string? value, CommandLineOptions options, List<string> errors)
    {
        switch (value?.ToLowerInvariant())
        {
            case null:
                return;

            case "yes":
                options.Overwrite = true;
                return;

            case "no":
                options.Overwrite = false;
                return;

            default:
                errors.Add($"Ключ /overwrite принимает yes или no, а получено '{value}'.");
                return;
        }
    }

    private static void ReadStubStyle(string? value, CommandLineOptions options, List<string> errors)
    {
        switch (value?.ToLowerInvariant())
        {
            case null:
                return;

            case "descriptive":
                options.StubStyle = StubStyle.Descriptive;
                return;

            case "sample":
                options.StubStyle = StubStyle.Sample;
                return;

            default:
                errors.Add($"Ключ /stub принимает descriptive или sample, а получено '{value}'.");
                return;
        }
    }
}
