using ExportMaster.Template.Text;

namespace ExportMaster.Template.Diagnostics;

/// <summary>
/// Замечание к шаблону с указанием места.
/// </summary>
public sealed record Diagnostic(
    DiagnosticSeverity Severity,
    DiagnosticCode Code,
    string Message,
    SourceSpan Span)
{
    /// <summary>Строка вида <c>(14,23): текст сообщения</c>.</summary>
    public override string ToString() => $"{Span}: {Message}";

    /// <summary>Замечание, делающее шаблон непригодным.</summary>
    public static Diagnostic Error(DiagnosticCode code, string message, SourceSpan span) =>
        new(DiagnosticSeverity.Error, code, message, span);

    /// <summary>Замечание, не мешающее сформировать файл.</summary>
    public static Diagnostic Warning(DiagnosticCode code, string message, SourceSpan span) =>
        new(DiagnosticSeverity.Warning, code, message, span);
}
