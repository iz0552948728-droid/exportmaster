using ExportMaster.Template.Text;

namespace ExportMaster.Template.Parsing.Ast;

/// <summary>
/// Участок текста, попадающий в выходной файл без изменений.
/// Экранирование <c>{{</c> и <c>}}</c> уже раскрыто.
/// </summary>
public sealed class TextNode : TemplateNode
{
    public TextNode(string text, SourceSpan span)
        : base(span) => Text = text;

    /// <summary>Текст в том виде, в каком он попадёт в результат.</summary>
    public string Text { get; }

    public override string ToString() => $"Text({Text.Length} симв.)";
}
