using ExportMaster.Engine.Jobs;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Обстановка, в которой вычисляется одно специальное поле.
/// </summary>
public sealed class RenderContext
{
    public RenderContext(OutputSettings settings)
    {
        Settings = settings;
    }

    /// <summary>Настройки выходного файла из заголовка шаблона.</summary>
    public OutputSettings Settings { get; }

    /// <summary>
    /// Число колонок в заглушке табличной подстановки. Реальные данные заменят
    /// это количеством значений соответствующей оси.
    /// </summary>
    public int StubTableColumns { get; init; } = 3;

    /// <summary>Число строк в заглушке табличной подстановки.</summary>
    public int StubTableRows { get; init; } = 4;
}
