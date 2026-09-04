using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Text;

namespace ExportMaster.Template.Parsing;

/// <summary>
/// Делит файл шаблона на заголовок и тело по тегам <c>[MDHEADER]</c> и <c>[/MDHEADER]</c>.
/// Реализует правила разбора заголовка из <c>docs/template-language.md</c>.
/// </summary>
internal static class TemplateSplitter
{
    internal const string OpeningTag = "[MDHEADER]";
    internal const string ClosingTag = "[/MDHEADER]";

    /// <summary>Результат деления: участки заголовка и тела.</summary>
    internal readonly record struct Result(TextRegion Header, TextRegion Body, bool HasHeader);

    /// <summary>
    /// Делит текст на заголовок и тело, добавляя замечания в <paramref name="diagnostics"/>.
    /// </summary>
    internal static Result Split(string text, List<Diagnostic> diagnostics)
    {
        // BOM отбрасывается: Блокнот и Visual Studio ставят его самостоятельно, и без
        // этого правила открывающий тег не совпал бы, а заголовок молча уехал бы в результат.
        var start = text.StartsWith('﻿') ? 1 : 0;

        var line = 1;
        var cursor = start;

        // Пустые строки перед открывающим тегом допускаются и отбрасываются.
        while (cursor < text.Length)
        {
            var current = ReadLine(text, cursor);
            if (!IsBlank(text, current))
            {
                break;
            }

            cursor = current.NextLineStart;
            line++;
        }

        if (cursor >= text.Length || !LineEquals(text, ReadLine(text, cursor), OpeningTag))
        {
            // Заголовок обязателен (ТЗ п. 4.4.1): без него неоткуда взять имя выходного файла.
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.MissingHeader,
                $"Шаблон должен начинаться со строки {OpeningTag}.",
                new SourceSpan(start, 0, 1, 1)));

            return new Result(TextRegion.Empty(line), new TextRegion(start, text.Length, 1), HasHeader: false);
        }

        var openingLine = ReadLine(text, cursor);
        var openingLineNumber = line;
        var headerStart = openingLine.NextLineStart;
        var headerLine = line + 1;

        cursor = headerStart;
        line = headerLine;

        while (cursor < text.Length)
        {
            var current = ReadLine(text, cursor);
            if (LineEquals(text, current, ClosingTag))
            {
                // Перевод строки, завершающий закрывающий тег, в тело не входит:
                // иначе каждый выходной файл начинался бы с пустой строки.
                return new Result(
                    new TextRegion(headerStart, current.Start, headerLine),
                    new TextRegion(current.NextLineStart, text.Length, line + 1),
                    HasHeader: true);
            }

            cursor = current.NextLineStart;
            line++;
        }

        // Молчаливый откат к «файлу без заголовка» недопустим: служебные строки
        // уехали бы в результат заказчику незамеченными.
        diagnostics.Add(Diagnostic.Error(
            DiagnosticCode.UnterminatedHeader,
            $"Заголовок открыт строкой {OpeningTag}, но не закрыт строкой {ClosingTag}.",
            new SourceSpan(openingLine.Start, OpeningTag.Length, openingLineNumber, 1)));

        return new Result(
            new TextRegion(headerStart, text.Length, headerLine),
            TextRegion.Empty(line),
            HasHeader: true);
    }

    private readonly record struct Line(int Start, int End, int NextLineStart);

    private static Line ReadLine(string text, int start)
    {
        var index = start;
        while (index < text.Length && text[index] != '\n' && text[index] != '\r')
        {
            index++;
        }

        var end = index;

        if (index < text.Length)
        {
            // CRLF считается одним разделителем строк.
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                index += 2;
            }
            else
            {
                index++;
            }
        }

        return new Line(start, end, index);
    }

    private static bool IsBlank(string text, Line line)
    {
        for (var index = line.Start; index < line.End; index++)
        {
            if (!char.IsWhiteSpace(text[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Строка состоит только из указанного тега. Окружающие пробелы и табуляции
    /// игнорируются, регистр не учитывается, ничего другого в строке быть не должно.
    /// </summary>
    private static bool LineEquals(string text, Line line, string tag)
    {
        var start = line.Start;
        var end = line.End;

        while (start < end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return end - start == tag.Length
            && string.Compare(text, start, tag, 0, tag.Length, StringComparison.OrdinalIgnoreCase) == 0;
    }
}
