using ExportMaster.Core;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Проверяет поля <c>GROUPVALUE</c> по списку осей отбора данных.
/// </summary>
/// <remarks>
/// <c>GROUPVALUE</c> подставляет текущее значение оси, по которой идёт разбиение.
/// Если ось не перечислена в <c>GROUPBY</c>, текущего значения у неё нет: она либо
/// осталась внутри двумерной вырезки, либо в данных её вовсе нет.
/// </remarks>
public static class GroupValueValidator
{
    private const string FieldName = "GROUPVALUE";

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
            if (!string.Equals(call.Name, FieldName, StringComparison.Ordinal))
            {
                continue;
            }

            Validate(call, groupBy, diagnostics);
        }
    }

    private static void Validate(CallNode call, IReadOnlyList<Dimensions> groupBy, List<Diagnostic> diagnostics)
    {
        if (call.Arguments.Count != 1)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Поле {FieldName} принимает ровно одно имя оси.",
                call.Span));

            return;
        }

        var argument = call.Arguments[0];

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
                $"Аргументом {FieldName} должно быть имя оси.",
                argument.Span));

            return;
        }

        if (!Enum.TryParse<Dimensions>(name, ignoreCase: false, out var dimension))
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"'{name}' не является осью. Допустимы: {string.Join(", ", Enum.GetNames<Dimensions>())}.",
                argument.Span));

            return;
        }

        if (!groupBy.Contains(dimension))
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Ось {name} не указана в GROUPBY, поэтому текущего значения у неё нет.",
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
