using System.Text;
using ExportMaster.Core;
using ExportMaster.Engine.Rendering;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Разбирает директивы заголовка шаблона.
/// </summary>
public sealed class HeaderSettings
{
    private HeaderSettings(
        CallNode? fileName,
        IReadOnlyList<Dimensions> groupBy,
        OutputSettings output)
    {
        FileName = fileName;
        GroupBy = groupBy;
        Output = output;
    }

    /// <summary>Способ формирования имени выходного файла (ТЗ п. 4.4.4).</summary>
    public CallNode? FileName { get; }

    /// <summary>Оси, по которым идёт итерирование (ТЗ п. 4.4.5).</summary>
    public IReadOnlyList<Dimensions> GroupBy { get; }

    /// <summary>Настройки выходного файла.</summary>
    public OutputSettings Output { get; }

    public static HeaderSettings Parse(IReadOnlyList<CallNode> header, List<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(diagnostics);

        CallNode? fileName = null;
        var groupBy = new List<Dimensions>();

        var decimalSeparator = ".";
        var axisFormats = new Dictionary<Dimensions, string>();
        var newLine = Environment.NewLine;
        Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        foreach (var field in header)
        {
            switch (field.Name)
            {
                case "FILENAME":
                    fileName = field;
                    break;

                case "GROUPBY":
                    ReadGroupBy(field, groupBy, diagnostics);
                    break;

                case "DELIMITER":
                    // Разделитель ячеек переехал в пятый аргумент TABLE: у разных
                    // таблиц одного файла он может различаться.
                    diagnostics.Add(Diagnostic.Error(
                        DiagnosticCode.ExpectedFieldName,
                        "DELIMITER больше не является директивой заголовка: "
                            + "разделитель ячеек задаётся пятым аргументом поля TABLE.",
                        field.Span));
                    break;

                case "DECIMAL":
                    decimalSeparator = ReadString(field, diagnostics) ?? decimalSeparator;
                    break;

                case "NEWLINE":
                    newLine = ReadNewLine(field, diagnostics) ?? newLine;
                    break;

                case "AXISFORMAT":
                    ReadAxisFormat(field, axisFormats, diagnostics);
                    break;

                case "ENCODING":
                    encoding = ReadEncoding(field, diagnostics) ?? encoding;
                    break;

                default:
                    // Состав полей заголовка ещё пополняется, поэтому неизвестное имя —
                    // предупреждение, а не отказ формировать файл.
                    diagnostics.Add(Diagnostic.Warning(
                        DiagnosticCode.ExpectedFieldName,
                        $"Неизвестная директива заголовка '{field.Name}'; она будет пропущена.",
                        field.Span));
                    break;
            }
        }

        if (fileName is null)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.MissingHeader,
                "В заголовке шаблона отсутствует поле FILENAME: неоткуда взять имя выходного файла.",
                default));
        }

        var output = new OutputSettings
        {
            DecimalSeparator = decimalSeparator,
            AxisFormats = axisFormats,
            NewLine = newLine,
            Encoding = encoding,
        };

        return new HeaderSettings(fileName, groupBy, output);
    }

    /// <summary>Собирает имя выходного файла, вычисляя аргументы поля FILENAME.</summary>
    public string BuildFileName(TemplateRenderer renderer, RenderContext context, List<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (FileName is null)
        {
            return string.Empty;
        }

        // Имя файла собирается короткими заглушками: полная запись поля
        // со скобками и кавычками нечитаема, а кавычки Windows в именах не допускает.
        var compact = new RenderContext(context.Settings)
        {
            StubAxisLength = context.StubAxisLength,
            CompactStubs = true,

            // Вырезка обязана перейти в этот контекст: без неё GROUPVALUE не
            // разрешится, имена всех групп совпадут и файлы затрут друг друга.
            Slice = context.Slice,
        };

        var builder = new StringBuilder();

        foreach (var argument in FileName.Arguments)
        {
            builder.Append(argument switch
            {
                StringArgument text => text.Value,
                NumberArgument number => number.Text,
                IdentifierArgument identifier => identifier.Name,
                CallArgument call => renderer.RenderCall(call.Call, compact, diagnostics),
                _ => string.Empty,
            });
        }

        return Sanitize(builder.ToString());
    }

    /// <summary>
    /// Убирает из имени символы, недопустимые в имени файла. Заглушки содержат
    /// угловые кавычки и скобки — сами по себе безобидные, но соседство с реальными
    /// значениями лучше проверять на пригодность заранее.
    /// </summary>
    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);

        foreach (var symbol in name)
        {
            builder.Append(Array.IndexOf(invalid, symbol) >= 0 ? '_' : symbol);
        }

        return builder.ToString().Trim();
    }

    private static void ReadGroupBy(CallNode field, List<Dimensions> target, List<Diagnostic> diagnostics)
    {
        foreach (var argument in field.Arguments)
        {
            var name = argument switch
            {
                IdentifierArgument identifier => identifier.Name,
                StringArgument text => text.Value,
                _ => null,
            };

            if (name is null)
            {
                diagnostics.Add(Diagnostic.Error(
                    DiagnosticCode.ExpectedArgument,
                    "Аргументом GROUPBY должно быть имя оси.",
                    argument.Span));

                continue;
            }

            if (!Enum.TryParse<Dimensions>(name, ignoreCase: false, out var dimension))
            {
                diagnostics.Add(Diagnostic.Error(
                    DiagnosticCode.ExpectedArgument,
                    $"'{name}' не является осью. Допустимы: {string.Join(", ", Enum.GetNames<Dimensions>())}.",
                    argument.Span));

                continue;
            }

            if (target.Contains(dimension))
            {
                diagnostics.Add(Diagnostic.Warning(
                    DiagnosticCode.ExpectedArgument,
                    $"Ось {name} указана в GROUPBY повторно.",
                    argument.Span));

                continue;
            }

            target.Add(dimension);
        }
    }

    /// <summary>
    /// <c>AXISFORMAT(&lt;ось&gt;, &lt;формат&gt;)</c> — сколько знаков после запятой
    /// выводить для значений этой оси.
    /// </summary>
    private static void ReadAxisFormat(
        CallNode field,
        Dictionary<Dimensions, string> target,
        List<Diagnostic> diagnostics)
    {
        if (field.Arguments.Count != 2)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                "Директива AXISFORMAT принимает имя оси и строку формата.",
                field.Span));

            return;
        }

        var name = field.Arguments[0] switch
        {
            StringArgument text => text.Value,
            IdentifierArgument identifier => identifier.Name,
            _ => null,
        };

        if (name is null || !Enum.TryParse<Dimensions>(name, ignoreCase: false, out var axis))
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Первым аргументом AXISFORMAT должно быть имя оси. Допустимы: "
                    + $"{string.Join(", ", Enum.GetNames<Dimensions>())}.",
                field.Arguments[0].Span));

            return;
        }

        if (field.Arguments[1] is not StringArgument format)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                "Вторым аргументом AXISFORMAT должна быть строка формата, например \"0.000\".",
                field.Arguments[1].Span));

            return;
        }

        target[axis] = format.Value;
    }

    private static string? ReadString(CallNode field, List<Diagnostic> diagnostics)
    {
        if (field.Arguments.Count == 1 && field.Arguments[0] is StringArgument text)
        {
            return text.Value;
        }

        diagnostics.Add(Diagnostic.Error(
            DiagnosticCode.ExpectedArgument,
            $"Директива {field.Name} принимает ровно одну строку.",
            field.Span));

        return null;
    }

    private static string? ReadNewLine(CallNode field, List<Diagnostic> diagnostics)
    {
        var value = ReadString(field, diagnostics);

        return value switch
        {
            null => null,
            "CRLF" => "\r\n",
            "LF" => "\n",
            _ => Unsupported(),
        };

        string? Unsupported()
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Неизвестный вид перевода строки '{value}'. Допустимы CRLF и LF.",
                field.Span));

            return null;
        }
    }

    private static Encoding? ReadEncoding(CallNode field, List<Diagnostic> diagnostics)
    {
        var value = ReadString(field, diagnostics);

        switch (value)
        {
            case null:
                return null;

            case "UTF-8":
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            case "UTF-8-BOM":
                // Excel при двойном клике по csv не спрашивает кодировку и без BOM
                // покажет кириллицу как мусор.
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

            case "CP1251":
                try
                {
                    return Encoding.GetEncoding(1251);
                }
                catch (ArgumentException)
                {
                    diagnostics.Add(Diagnostic.Error(
                        DiagnosticCode.ExpectedArgument,
                        "Кодировка CP1251 недоступна в этой среде исполнения.",
                        field.Span));

                    return null;
                }

            default:
                diagnostics.Add(Diagnostic.Error(
                    DiagnosticCode.ExpectedArgument,
                    $"Неизвестная кодировка '{value}'. Допустимы UTF-8, UTF-8-BOM, CP1251.",
                    field.Span));

                return null;
        }
    }
}
