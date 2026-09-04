using ExportMaster.Template.Text;

namespace ExportMaster.Template.Parsing.Ast;

/// <summary>Узел разобранного шаблона.</summary>
public abstract class TemplateNode
{
    private protected TemplateNode(SourceSpan span) => Span = span;

    /// <summary>Место узла в файле шаблона.</summary>
    public SourceSpan Span { get; }
}
