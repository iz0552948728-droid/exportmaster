namespace ExportMaster.Template.Parsing;

/// <summary>
/// Участок файла шаблона, разбираемый как последовательность узлов.
/// Хранит номер первой строки, чтобы позиции в сообщениях были
/// от начала файла, а не от начала участка.
/// </summary>
internal readonly record struct TextRegion(int Start, int End, int Line)
{
    public static TextRegion Empty(int line) => new(0, 0, line);

    public bool IsEmpty => Start >= End;
}
