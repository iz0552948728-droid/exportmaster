namespace ExportMaster.Template.Parsing.Ast;

/// <summary>
/// Разобранный шаблон: заголовок и тело.
/// </summary>
public sealed class TemplateDocument
{
    public TemplateDocument(
        IReadOnlyList<CallNode> header,
        IReadOnlyList<TemplateNode> body,
        bool hasHeader)
    {
        Header = header;
        Body = body;
        HasHeader = hasHeader;
    }

    /// <summary>
    /// Поля заголовка: способ формирования имени файла, отбор данных, настройки вывода.
    /// В выходной файл заголовок не попадает (ТЗ п. 4.4.1).
    /// </summary>
    public IReadOnlyList<CallNode> Header { get; }

    /// <summary>Тело: текст будущего файла вперемежку со специальными полями.</summary>
    public IReadOnlyList<TemplateNode> Body { get; }

    /// <summary>Был ли в файле заголовок. Отсутствие — ошибка, но разбор тела продолжается.</summary>
    public bool HasHeader { get; }
}
