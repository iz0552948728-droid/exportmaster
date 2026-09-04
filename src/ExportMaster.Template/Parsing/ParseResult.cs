using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Template.Parsing;

/// <summary>
/// Итог разбора шаблона: дерево и замечания.
/// </summary>
/// <remarks>
/// Разбор не бросает исключений: он собирает замечания и продолжает, чтобы автор
/// шаблона увидел все ошибки разом, а не по одной за прогон. Дерево при наличии
/// ошибок остаётся неполным и для формирования файла непригодно.
/// </remarks>
public sealed class ParseResult
{
    internal ParseResult(TemplateDocument document, IReadOnlyList<Diagnostic> diagnostics)
    {
        Document = document;
        Diagnostics = diagnostics;
    }

    public TemplateDocument Document { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Есть ли замечания, делающие шаблон непригодным.</summary>
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
