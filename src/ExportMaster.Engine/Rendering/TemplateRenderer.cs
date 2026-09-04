using System.Globalization;
using System.Text;
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
        if (call.Arguments.Count is < 4 or > 5)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Поле {TableField} принимает от четырёх до пяти аргументов: "
                    + "заголовки колонок, заголовки строк, функцию данных, формат и, необязательно, угловую ячейку.",
                call.Span));

            return _resolver.Resolve(call, context).Text;
        }

        var columnAxis = ArgumentText(call.Arguments[0]);
        var rowAxis = ArgumentText(call.Arguments[1]);
        var corner = call.Arguments.Count == 5 ? ArgumentText(call.Arguments[4]) : string.Empty;

        var delimiter = context.Settings.Delimiter;
        var builder = new StringBuilder();

        // Строка заголовков: значения оси колонок приходят из данных, вписать их
        // в шаблон вручную нельзя, поэтому их печатает сама таблица.
        builder.Append(corner);
        for (var column = 1; column <= context.StubTableColumns; column++)
        {
            builder.Append(delimiter).Append(Header(columnAxis, column, context));
        }

        for (var row = 1; row <= context.StubTableRows; row++)
        {
            // Перевод строки ставится перед строкой, а не после: после последней
            // строки таблицы его быть не должно — его поставит сам шаблон.
            builder.Append('\n').Append(Header(rowAxis, row, context));

            for (var column = 1; column <= context.StubTableColumns; column++)
            {
                builder.Append(delimiter).Append(RenderCell(call, row, column, context, diagnostics));
            }
        }

        return builder.ToString();
    }

    private string RenderCell(
        CallNode table,
        int row,
        int column,
        RenderContext context,
        List<Diagnostic> diagnostics)
    {
        var function = ArgumentText(table.Arguments[2]);
        var format = table.Arguments[3];

        // Номера строки и колонки входят в состав ячейки, чтобы значения заглушек
        // различались: одинаковые числа по всей таблице скрыли бы сбитые колонки.
        var cell = new CallNode(
            function,
            [
                new NumberArgument(row, row.ToString(CultureInfo.InvariantCulture), table.Span),
                new NumberArgument(column, column.ToString(CultureInfo.InvariantCulture), table.Span),
            ],
            table.Span);

        var value = _resolver.Resolve(cell, context);

        if (value.Number is { } number && format is StringArgument formatText)
        {
            return number.ToString(formatText.Value, context.Settings.NumberFormat);
        }

        _ = diagnostics;
        return value.Text;
    }

    private string Header(string axis, int index, RenderContext context) =>
        _resolver is StubValueResolver stub
            ? stub.AxisHeader(axis, index, context)
            : $"{axis}{index}";

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
