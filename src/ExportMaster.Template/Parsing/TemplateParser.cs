using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Lexing;
using ExportMaster.Template.Parsing.Ast;
using ExportMaster.Template.Text;

namespace ExportMaster.Template.Parsing;

/// <summary>
/// Разбирает файл шаблона в дерево.
/// </summary>
public sealed class TemplateParser
{
    private readonly List<Diagnostic> _diagnostics;
    private readonly List<Token> _tokens;
    private int _index;

    private TemplateParser(List<Token> tokens, List<Diagnostic> diagnostics)
    {
        _tokens = tokens;
        _diagnostics = diagnostics;
    }

    /// <summary>Разбирает шаблон целиком.</summary>
    public static ParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var diagnostics = new List<Diagnostic>();
        var split = TemplateSplitter.Split(text, diagnostics);

        var header = ParseHeader(text, split.Header, diagnostics);
        var body = ParseRegion(text, split.Body, diagnostics);

        return new ParseResult(new TemplateDocument(header, body, split.HasHeader), diagnostics);
    }

    /// <summary>
    /// Заголовок содержит только специальные поля: произвольный текст в нём — ошибка,
    /// так ловятся опечатки в именах полей вместо их молчаливого попадания в результат.
    /// </summary>
    private static IReadOnlyList<CallNode> ParseHeader(
        string text,
        TextRegion region,
        List<Diagnostic> diagnostics)
    {
        var nodes = ParseRegion(text, region, diagnostics);
        var fields = new List<CallNode>();

        foreach (var node in nodes)
        {
            switch (node)
            {
                case CallNode call:
                    fields.Add(call);
                    break;

                case TextNode textNode when string.IsNullOrWhiteSpace(textNode.Text):
                    break;

                case TextNode textNode:
                    diagnostics.Add(Diagnostic.Error(
                        DiagnosticCode.UnexpectedTextInHeader,
                        $"В заголовке допустимы только специальные поля, а встречено: '{Shorten(textNode.Text)}'.",
                        textNode.Span));
                    break;
            }
        }

        return fields;
    }

    private static List<TemplateNode> ParseRegion(string text, TextRegion region, List<Diagnostic> diagnostics)
    {
        if (region.IsEmpty)
        {
            return [];
        }

        var lexer = new TemplateLexer(text, region.Start, region.End, region.Line, diagnostics);
        var parser = new TemplateParser(lexer.Tokenize(), diagnostics);
        return parser.ParseNodes();
    }

    private Token Current => _tokens[_index];

    private Token Peek => _index + 1 < _tokens.Count ? _tokens[_index + 1] : _tokens[^1];

    private Token Advance() => _tokens[_index++];

    private List<TemplateNode> ParseNodes()
    {
        var nodes = new List<TemplateNode>();

        while (Current.Kind != TokenKind.EndOfFile)
        {
            switch (Current.Kind)
            {
                case TokenKind.Text:
                {
                    var token = Advance();
                    nodes.Add(new TextNode(token.Text, token.Span));
                    break;
                }

                case TokenKind.LeftBrace:
                {
                    var call = ParseBracedCall();
                    if (call is not null)
                    {
                        nodes.Add(call);
                    }

                    break;
                }

                default:
                    // Разбор поля прервался на ошибке — пропускаем лексему и продолжаем,
                    // чтобы собрать остальные замечания за один прогон.
                    Advance();
                    break;
            }
        }

        return nodes;
    }

    /// <summary>Разбирает поле вида <c>{ИМЯ(...)}</c>.</summary>
    private CallNode? ParseBracedCall()
    {
        var opening = Advance();

        if (Current.Kind != TokenKind.Identifier)
        {
            _diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedFieldName,
                "После открывающей фигурной скобки ожидается имя специального поля.",
                Current.Span));

            return null;
        }

        var call = ParseCallBody(opening.Span);
        if (call is null)
        {
            return null;
        }

        if (Current.Kind != TokenKind.RightBrace)
        {
            _diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedClosingBrace,
                $"Специальное поле '{call.Name}' не закрыто фигурной скобкой.",
                Current.Span));

            return call;
        }

        var closing = Advance();
        return new CallNode(call.Name, call.Arguments, Between(opening.Span, closing.Span));
    }

    /// <summary>
    /// Разбирает <c>ИМЯ(аргументы)</c> без обрамляющих фигурных скобок.
    /// </summary>
    /// <param name="start">
    /// Начало поля: открывающая фигурная скобка, если она была, иначе само имя.
    /// </param>
    private CallNode? ParseCallBody(SourceSpan start)
    {
        var name = Advance();

        if (Current.Kind != TokenKind.LeftParenthesis)
        {
            _diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedOpeningParenthesis,
                $"После имени '{name.Text}' ожидается открывающая круглая скобка.",
                Current.Span));

            return null;
        }

        Advance();

        var arguments = new List<Argument>();

        if (Current.Kind != TokenKind.RightParenthesis)
        {
            while (true)
            {
                var argument = ParseArgument();
                if (argument is null)
                {
                    return null;
                }

                arguments.Add(argument);

                if (Current.Kind != TokenKind.Comma)
                {
                    break;
                }

                Advance();
            }
        }

        if (Current.Kind != TokenKind.RightParenthesis)
        {
            _diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedClosingParenthesis,
                $"Список аргументов поля '{name.Text}' не закрыт круглой скобкой.",
                Current.Span));

            return null;
        }

        var closing = Advance();
        return new CallNode(name.Text, arguments, Between(start, closing.Span));
    }

    private Argument? ParseArgument()
    {
        switch (Current.Kind)
        {
            case TokenKind.String:
            {
                var token = Advance();
                return new StringArgument(token.Text, token.Span);
            }

            case TokenKind.Number:
            {
                var token = Advance();
                return new NumberArgument(token.Number, token.Text, token.Span);
            }

            case TokenKind.LeftBrace:
            {
                var call = ParseBracedCall();
                return call is null ? null : new CallArgument(call);
            }

            case TokenKind.Identifier when Peek.Kind == TokenKind.LeftParenthesis:
            {
                // Внутри списка аргументов фигурные скобки необязательны: записи X(...)
                // и {X(...)} равнозначны. Это согласует таблицу синтаксиса ТЗ п. 4.4.3.3
                // с приведённым там же примером AMP({VALUE(...)}).
                var call = ParseCallBody(Current.Span);
                return call is null ? null : new CallArgument(call);
            }

            case TokenKind.Identifier:
            {
                var token = Advance();
                return new IdentifierArgument(token.Text, token.Span);
            }

            default:
                _diagnostics.Add(Diagnostic.Error(
                    DiagnosticCode.ExpectedArgument,
                    "Здесь ожидается аргумент: строка, число, имя или вложенное поле.",
                    Current.Span));

                return null;
        }
    }

    private static SourceSpan Between(SourceSpan start, SourceSpan end) =>
        start with { Length = end.End - start.Start };

    private static string Shorten(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 40 ? trimmed : trimmed[..40] + "…";
    }
}
