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

    internal static Diagnostic Error(DiagnosticCode code, string message, SourceSpan span) =>
        new(DiagnosticSeverity.Error, code, message, span);

    internal static Diagnostic Warning(DiagnosticCode code, string message, SourceSpan span) =>
        new(DiagnosticSeverity.Warning, code, message, span);
}
