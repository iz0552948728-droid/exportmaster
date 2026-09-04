namespace ExportMaster.Template.Text;

/// <summary>
/// Участок исходного текста шаблона.
/// </summary>
/// <param name="Start">Смещение от начала файла, символов.</param>
/// <param name="Length">Длина участка, символов.</param>
/// <param name="Line">Номер строки, начиная с единицы.</param>
/// <param name="Column">Номер колонки, начиная с единицы.</param>
/// <remarks>
/// Шаблоны пишутся вручную, поэтому каждый узел дерева несёт своё место в файле:
/// без него сообщение об ошибке бесполезно.
/// </remarks>
public readonly record struct SourceSpan(int Start, int Length, int Line, int Column)
{
    /// <summary>Смещение первого символа за участком.</summary>
    public int End => Start + Length;

    /// <summary>Позиция в формате, привычном для сообщений компилятора.</summary>
    public override string ToString() => $"({Line},{Column})";
}
