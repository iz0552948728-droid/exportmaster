namespace ExportMaster.Template.Diagnostics;

/// <summary>Тяжесть замечания к шаблону.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Шаблон пригоден к использованию, но что-то выглядит подозрительно.</summary>
    Warning,

    /// <summary>Шаблон непригоден: результат по нему сформировать нельзя.</summary>
    Error,
}
