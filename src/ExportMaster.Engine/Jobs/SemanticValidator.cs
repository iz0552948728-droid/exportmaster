using ExportMaster.Core;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Проверяет смысл шаблона по осям: то, что нельзя проверить разбором синтаксиса,
/// но можно — до чтения файлов данных.
/// </summary>
/// <remarks>
/// <para>
/// Матрица разбивается ключом <c>GROUPBY</c> так, чтобы в вырезке осталось ровно
/// два измерения, и они же становятся строками и колонками табличной подстановки.
/// Отсюда два правила, проверяемые без данных: ось таблицы не может быть осью
/// разбиения, а <c>GROUPVALUE</c> может ссылаться только на ось разбиения.
/// </para>
/// <para>
/// Третье правило — число осей <c>GROUPBY</c> равно размерности матрицы минус два —
/// проверяется только по описателю из файла данных и появится вместе с его чтением.
/// </para>
/// </remarks>
public static class SemanticValidator
{
    private const string GroupValueField = "GROUPVALUE";
    private const string TableField = "TABLE";

    public static void Validate(
        TemplateDocument document,
        IReadOnlyList<Dimensions> groupBy,
        List<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(groupBy);
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (var call in EnumerateCalls(document))
        {
            switch (call.Name)
            {
                case GroupValueField:
                    ValidateGroupValue(call, groupBy, diagnostics);
                    break;

                case TableField:
                    ValidateTable(call, groupBy, diagnostics);
                    break;
            }
        }
    }

    /// <summary>
    /// Оси таблицы — это те измерения, что остались в вырезке. Ось, по которой шло
    /// разбиение, в вырезке уже зафиксирована и осью таблицы быть не может.
    /// </summary>
    private static void ValidateTable(
        CallNode call,
        IReadOnlyList<Dimensions> groupBy,
        List<Diagnostic> diagnostics)
    {
        if (call.Arguments.Count < 2)
        {
            return;
        }

        var columns = ReadAxis(call.Arguments[0], diagnostics);
        var rows = ReadAxis(call.Arguments[1], diagnostics);

        if (columns is null || rows is null)
        {
            return;
        }

        if (columns == rows)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Колонки и строки таблицы не могут идти по одной оси {columns}.",
                call.Arguments[1].Span));
        }

        CheckNotGrouped(columns.Value, call.Arguments[0], groupBy, diagnostics);
        CheckNotGrouped(rows.Value, call.Arguments[1], groupBy, diagnostics);
    }

    private static void CheckNotGrouped(
        Dimensions axis,
        Argument argument,
        IReadOnlyList<Dimensions> groupBy,
        List<Diagnostic> diagnostics)
    {
        if (groupBy.Contains(axis))
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Ось {axis} указана в GROUPBY, поэтому в вырезке она зафиксирована "
                    + "и осью таблицы быть не может.",
                argument.Span));
        }
    }

    /// <summary>Читает имя оси из аргумента, сообщая о непригодных значениях.</summary>
    private static Dimensions? ReadAxis(Argument argument, List<Diagnostic> diagnostics)
    {
        var name = argument switch
        {
            StringArgument text => text.Value,
            IdentifierArgument identifier => identifier.Name,
            _ => null,
        };

        if (name is null)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                "Здесь ожидается имя оси.",
                argument.Span));

            return null;
        }

        if (!Enum.TryParse<Dimensions>(name, ignoreCase: false, out var axis))
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"'{name}' не является осью. Допустимы: {string.Join(", ", Enum.GetNames<Dimensions>())}.",
                argument.Span));

            return null;
        }

        return axis;
    }

    private static void ValidateGroupValue(
        CallNode call,
        IReadOnlyList<Dimensions> groupBy,
        List<Diagnostic> diagnostics)
    {
        if (call.Arguments.Count != 1)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Поле {GroupValueField} принимает ровно одно имя оси.",
                call.Span));

            return;
        }

        var argument = call.Arguments[0];
        var axis = ReadAxis(argument, diagnostics);

        if (axis is { } dimension && !groupBy.Contains(dimension))
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Ось {dimension} не указана в GROUPBY, поэтому текущего значения у неё нет.",
                argument.Span));
        }
    }

    /// <summary>Обходит все специальные поля шаблона, включая вложенные в аргументы.</summary>
    private static IEnumerable<CallNode> EnumerateCalls(TemplateDocument document)
    {
        foreach (var field in document.Header)
        {
            foreach (var call in Descend(field))
            {
                yield return call;
            }
        }

        foreach (var node in document.Body.OfType<CallNode>())
        {
            foreach (var call in Descend(node))
            {
                yield return call;
            }
        }
    }

    private static IEnumerable<CallNode> Descend(CallNode call)
    {
        yield return call;

        foreach (var nested in call.Arguments.OfType<CallArgument>())
        {
            foreach (var inner in Descend(nested.Call))
            {
                yield return inner;
            }
        }
    }
}
