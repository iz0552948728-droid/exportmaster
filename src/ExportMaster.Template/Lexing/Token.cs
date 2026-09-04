using ExportMaster.Template.Text;

namespace ExportMaster.Template.Lexing;

/// <summary>
/// Лексема шаблона.
/// </summary>
/// <param name="Kind">Вид лексемы.</param>
/// <param name="Text">
/// Содержимое: раскрытый текст, значение строкового литерала, имя или запись числа.
/// Для знаков препинания — сам знак.
/// </param>
/// <param name="Span">Место в файле.</param>
/// <param name="Number">Значение числового литерала; для остальных видов не используется.</param>
public readonly record struct Token(TokenKind Kind, string Text, SourceSpan Span, double Number = 0)
{
    public override string ToString() => $"{Kind} {Span}";
}
