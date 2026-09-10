using ExportMaster.Engine.Data;
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
    /// Число точек по каждой оси в заглушке табличной подстановки. Реальные данные
    /// заменят его длиной соответствующей оси из файла.
    /// </summary>
    public int StubAxisLength { get; init; } = 4;

    /// <summary>
    /// Собирать заглушки в короткой форме. Включается при формировании имени файла:
    /// полная запись поля со скобками и кавычками даёт нечитаемое имя, а часть
    /// её символов вдобавок недопустима в именах файлов Windows.
    /// </summary>
    public bool CompactStubs { get; init; }

    /// <summary>
    /// Вырезка матрицы, для которой формируется текущий файл. Пока файлы данных
    /// не заданы, остаётся пустой, и значения подставляются заглушками.
    /// </summary>
    public GroupSlice? Slice { get; init; }
}
