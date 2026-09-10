using System.Text;
using ExportMaster.Core;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Собирает содержимое выходного файла по разобранному шаблону.
/// </summary>
/// <remarks>
/// Текст тела переносится в результат без изменений; заменяются только специальные
/// поля. Структурно рендерер знает лишь два поля — <c>FORMAT</c> и <c>TABLE</c>, —
/// остальные передаёт источнику значений. Так добавление функции не требует правки
/// рендерера, а ТЗ прямо предупреждает, что список будет расширяться.
/// </remarks>
public sealed class TemplateRenderer
{
    private const string FormatField = "FORMAT";
    private const string TableField = "TABLE";

    private readonly IValueResolver _resolver;

    public TemplateRenderer(IValueResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>Собирает тело выходного файла.</summary>
    public string Render(
        IReadOnlyList<TemplateNode> body,
        RenderContext context,
        List<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var builder = new StringBuilder();

        foreach (var node in body)
        {
            switch (node)
            {
                case TextNode text:
                    builder.Append(text.Text);
                    break;

                case CallNode call:
                    builder.Append(RenderCall(call, context, diagnostics));
                    break;
            }
        }

        return NormalizeNewLines(builder.ToString(), context.Settings.NewLine);
    }

    /// <summary>Вычисляет одно поле в текст.</summary>
    public string RenderCall(CallNode call, RenderContext context, List<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(call);

        return call.Name switch
        {
            TableField => RenderTable(call, context, diagnostics),
            _ => Evaluate(call, context, diagnostics).Text,
        };
    }

    private ResolvedValue Evaluate(CallNode call, RenderContext context, List<Diagnostic> diagnostics)
    {
        if (string.Equals(call.Name, FormatField, StringComparison.Ordinal))
        {
            return EvaluateFormat(call, context, diagnostics);
        }

        return _resolver.Resolve(call, context);
    }

    /// <summary>
    /// <c>FORMAT(строка формата, значение)</c>. Форматировать можно только число:
    /// если значение им не является — как заглушка-описание, — поле описывает само себя.
    /// </summary>
    private ResolvedValue EvaluateFormat(CallNode call, RenderContext context, List<Diagnostic> diagnostics)
    {
        if (call.Arguments.Count != 2)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Поле {FormatField} принимает два аргумента: строку формата и значение.",
                call.Span));

            return _resolver.Resolve(call, context);
        }

        if (call.Arguments[0] is not StringArgument format)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Первым аргументом {FormatField} должна быть строка формата.",
                call.Arguments[0].Span));

            return _resolver.Resolve(call, context);
        }

        var value = EvaluateArgument(call.Arguments[1], context, diagnostics);

        return value.Number is { } number
            ? ResolvedValue.FromNumber(number, number.ToString(format.Value, context.Settings.NumberFormat))
            : _resolver.Resolve(call, context);
    }

    private ResolvedValue EvaluateArgument(Argument argument, RenderContext context, List<Diagnostic> diagnostics) =>
        argument switch
        {
            CallArgument call => Evaluate(call.Call, context, diagnostics),
            StringArgument text => ResolvedValue.FromText(text.Value),
            NumberArgument number => ResolvedValue.FromNumber(
                number.Value,
                number.Value.ToString(context.Settings.NumberFormat)),
            IdentifierArgument identifier => ResolvedValue.FromText(identifier.Name),
            _ => ResolvedValue.FromText(string.Empty),
        };

    /// <summary>
    /// <c>TABLE(колонки, строки, функция, формат [, угол])</c> — единственное поле,
    /// порождающее сетку, а не одно значение. Поэтому оно само расставляет разделители
    /// ячеек и переводы строк.
    /// </summary>
    private string RenderTable(CallNode call, RenderContext context, List<Diagnostic> diagnostics)
    {
        if (call.Arguments.Count != 5)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Поле {TableField} принимает пять аргументов: заголовки колонок, "
                    + "заголовки строк, функцию данных, формат и разделитель ячеек.",
                call.Span));

            return _resolver.Resolve(call, context).Text;
        }

        var columnAxis = ReadAxis(call.Arguments[0]);
        var rowAxis = ReadAxis(call.Arguments[1]);

        if (columnAxis is null || rowAxis is null)
        {
            // Имена осей проверяет SemanticValidator; здесь просто нечего рисовать.
            return _resolver.Resolve(call, context).Text;
        }

        // Разделитель ячеек задаётся пятым аргументом: у разных таблиц одного
        // файла он может различаться, поэтому в заголовок шаблона его не выносят.
        var delimiter = ArgumentText(call.Arguments[4]);
        var function = ArgumentText(call.Arguments[2]);
        var format = call.Arguments[3] as StringArgument;

        var columns = _resolver.AxisLength(columnAxis.Value, context);
        var rows = _resolver.AxisLength(rowAxis.Value, context);

        var builder = new StringBuilder();

        // Строка заголовков: значения оси колонок приходят из данных, вписать их
        // в шаблон вручную нельзя, поэтому их печатает сама таблица. Ячейка на
        // пересечении заголовков пуста.
        for (var column = 0; column < columns; column++)
        {
            builder.Append(delimiter).Append(_resolver.AxisHeader(columnAxis.Value, column, context));
        }

        for (var row = 0; row < rows; row++)
        {
            // Перевод строки ставится перед строкой, а не после: после последней
            // строки таблицы его быть не должно — его поставит сам шаблон.
            builder.Append('\n').Append(_resolver.AxisHeader(rowAxis.Value, row, context));

            for (var column = 0; column < columns; column++)
            {
                var value = _resolver.Cell(function, columnAxis.Value, column, rowAxis.Value, row, context);

                builder.Append(delimiter).Append(
                    value.Number is { } number && format is not null
                        ? number.ToString(format.Value, context.Settings.NumberFormat)
                        : value.Text);
            }
        }

        return builder.ToString();
    }

    private static Dimensions? ReadAxis(Argument argument)
    {
        var name = argument switch
        {
            StringArgument text => text.Value,
            IdentifierArgument identifier => identifier.Name,
            _ => null,
        };

        return name is not null && Enum.TryParse<Dimensions>(name, ignoreCase: false, out var axis)
            ? axis
            : null;
    }

    private static string ArgumentText(Argument argument) => argument switch
    {
        StringArgument text => text.Value,
        IdentifierArgument identifier => identifier.Name,
        NumberArgument number => number.Text,
        CallArgument call => CallText.Format(call.Call),
        _ => string.Empty,
    };

    /// <summary>
    /// Приводит переводы строк к выбранному виду. Шаблон мог быть сохранён в редакторе
    /// с любым соглашением, а результат должен быть предсказуем.
    /// </summary>
    private static string NormalizeNewLines(string text, string newLine)
    {
        var builder = new StringBuilder(text.Length);

        for (var index = 0; index < text.Length; index++)
        {
            var symbol = text[index];

            if (symbol == '\r')
            {
                builder.Append(newLine);

                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                continue;
            }

            if (symbol == '\n')
            {
                builder.Append(newLine);
                continue;
            }

            builder.Append(symbol);
        }

        return builder.ToString();
    }
}
