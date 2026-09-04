using System.Globalization;
using System.Text;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Text;

namespace ExportMaster.Template.Lexing;

/// <summary>
/// Разбирает участок шаблона на лексемы.
/// </summary>
/// <remarks>
/// Лексер работает в двух режимах и переключается между ними сам, считая вложенность
/// фигурных скобок. Вне поля значим только текст и экранирование <c>{{</c> и <c>}}</c>;
/// внутри поля текст разбирается на имена, числа, строки и знаки препинания.
/// Скобки внутри строкового литерала на вложенность не влияют — литерал читается целиком.
/// </remarks>
internal sealed class TemplateLexer
{
    private readonly string _text;
    private readonly int _end;
    private readonly List<Diagnostic> _diagnostics;

    private int _position;
    private int _line;
    private int _column;

    /// <summary>Вложенность фигурных скобок: 0 — текст, больше нуля — внутри поля.</summary>
    private int _depth;

    internal TemplateLexer(string text, int start, int end, int startLine, List<Diagnostic> diagnostics)
    {
        _text = text;
        _end = end;
        _diagnostics = diagnostics;
        _position = start;
        _line = startLine;
        _column = 1;
    }

    private bool AtEnd => _position >= _end;

    private char Current => _text[_position];

    private char Next => _position + 1 < _end ? _text[_position + 1] : '\0';

    /// <summary>Разбирает участок целиком. Последней всегда идёт лексема <see cref="TokenKind.EndOfFile"/>.</summary>
    internal List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!AtEnd)
        {
            var token = _depth == 0 ? ReadText() : ReadFieldToken();
            if (token is not null)
            {
                tokens.Add(token.Value);
            }
        }

        if (_depth > 0)
        {
            _diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.UnterminatedField,
                "Специальное поле не закрыто фигурной скобкой.",
                Here(0)));
        }

        tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, Here(0)));
        return tokens;
    }

    /// <summary>
    /// Читает текст вне поля до ближайшего открывающего <c>{</c> или до конца участка.
    /// </summary>
    private Token? ReadText()
    {
        var startSpan = Here(0);
        var builder = new StringBuilder();

        while (!AtEnd)
        {
            if (Current == '{')
            {
                if (Next == '{')
                {
                    builder.Append('{');
                    Advance();
                    Advance();
                    continue;
                }

                break;
            }

            if (Current == '}')
            {
                if (Next == '}')
                {
                    builder.Append('}');
                    Advance();
                    Advance();
                    continue;
                }

                // Литеральные скобки пишутся удвоенными (ТЗ п. 4.4.3.1), поэтому одиночная
                // закрывающая — почти наверняка описка или незакрытое поле выше.
                _diagnostics.Add(Diagnostic.Error(
                    DiagnosticCode.UnbalancedClosingBrace,
                    "Закрывающая фигурная скобка без открывающей. Литеральная скобка пишется как }}.",
                    Here(1)));

                builder.Append('}');
                Advance();
                continue;
            }

            builder.Append(Current);
            Advance();
        }

        if (builder.Length == 0)
        {
            // Текста не было — участок начинается сразу с поля.
            return ReadLeftBrace();
        }

        return new Token(
            TokenKind.Text,
            builder.ToString(),
            startSpan with { Length = _position - startSpan.Start });
    }

    private Token? ReadLeftBrace()
    {
        if (AtEnd)
        {
            return null;
        }

        var span = Here(1);
        Advance();
        _depth++;
        return new Token(TokenKind.LeftBrace, "{", span);
    }

    /// <summary>Читает одну лексему внутри специального поля.</summary>
    private Token? ReadFieldToken()
    {
        SkipWhitespace();

        if (AtEnd)
        {
            return null;
        }

        var span = Here(1);
        var current = Current;

        switch (current)
        {
            case '{':
                Advance();
                _depth++;
                return new Token(TokenKind.LeftBrace, "{", span);

            case '}':
                Advance();
                _depth--;
                return new Token(TokenKind.RightBrace, "}", span);

            case '(':
                Advance();
                return new Token(TokenKind.LeftParenthesis, "(", span);

            case ')':
                Advance();
                return new Token(TokenKind.RightParenthesis, ")", span);

            case ',':
                Advance();
                return new Token(TokenKind.Comma, ",", span);

            case '"':
                return ReadString();
        }

        if (current is '+' or '-' || char.IsAsciiDigit(current))
        {
            return ReadNumber();
        }

        if (char.IsAsciiLetter(current) || current == '_')
        {
            return ReadIdentifier();
        }

        _diagnostics.Add(Diagnostic.Error(
            DiagnosticCode.UnexpectedCharacter,
            $"Символ '{current}' недопустим внутри специального поля.",
            span));

        Advance();
        return null;
    }

    /// <summary>Читает строковый литерал; удвоенная кавычка внутри означает одну кавычку.</summary>
    private Token ReadString()
    {
        var startSpan = Here(0);
        var builder = new StringBuilder();

        Advance();

        while (!AtEnd)
        {
            if (Current == '"')
            {
                if (Next == '"')
                {
                    builder.Append('"');
                    Advance();
                    Advance();
                    continue;
                }

                Advance();
                return new Token(
                    TokenKind.String,
                    builder.ToString(),
                    startSpan with { Length = _position - startSpan.Start });
            }

            builder.Append(Current);
            Advance();
        }

        _diagnostics.Add(Diagnostic.Error(
            DiagnosticCode.UnterminatedString,
            "Строковый литерал не закрыт кавычкой.",
            startSpan with { Length = _position - startSpan.Start }));

        return new Token(
            TokenKind.String,
            builder.ToString(),
            startSpan with { Length = _position - startSpan.Start });
    }

    private Token ReadNumber()
    {
        var startSpan = Here(0);

        if (Current is '+' or '-')
        {
            Advance();
        }

        ReadDigits();

        if (!AtEnd && Current == '.')
        {
            Advance();
            ReadDigits();
        }

        if (!AtEnd && (Current == 'e' || Current == 'E'))
        {
            Advance();

            if (!AtEnd && (Current is '+' or '-'))
            {
                Advance();
            }

            ReadDigits();
        }

        var span = startSpan with { Length = _position - startSpan.Start };
        var raw = _text[span.Start..span.End];

        // Разбор числовых литералов шаблона не зависит от локали машины:
        // разделитель целой и дробной части в самом шаблоне — всегда точка.
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            _diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.InvalidNumber,
                $"'{raw}' не является числом.",
                span));
        }

        return new Token(TokenKind.Number, raw, span, value);
    }

    private void ReadDigits()
    {
        while (!AtEnd && char.IsAsciiDigit(Current))
        {
            Advance();
        }
    }

    private Token ReadIdentifier()
    {
        var startSpan = Here(0);

        while (!AtEnd && (char.IsAsciiLetterOrDigit(Current) || Current == '_'))
        {
            Advance();
        }

        var span = startSpan with { Length = _position - startSpan.Start };
        return new Token(TokenKind.Identifier, _text[span.Start..span.End], span);
    }

    private void SkipWhitespace()
    {
        while (!AtEnd && char.IsWhiteSpace(Current))
        {
            Advance();
        }
    }

    private SourceSpan Here(int length) => new(_position, length, _line, _column);

    private void Advance()
    {
        if (Current == '\n')
        {
            _line++;
            _column = 1;
        }
        else if (Current == '\r')
        {
            // CRLF считается одним разделителем: перевод строки засчитается на '\n'.
            if (Next != '\n')
            {
                _line++;
                _column = 1;
            }
        }
        else
        {
            _column++;
        }

        _position++;
    }
}
