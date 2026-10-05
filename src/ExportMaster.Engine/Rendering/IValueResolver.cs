using ExportMaster.Core;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Источник значений для специальных полей.
/// </summary>
/// <remarks>
/// Главный шов конструкции. На этапе без данных подставляется
/// <see cref="StubValueResolver"/>, при наличии файлов измерений — читающая их
/// реализация. Разбор шаблона, рендерер, командная строка и формат вывода
/// при этой замене не меняются.
/// </remarks>
public interface IValueResolver
{
    /// <summary>
    /// Замечания, накопленные при подстановке значений.
    /// </summary>
    /// <remarks>
    /// Список только растёт, и по его длине до и после формирования файла видно,
    /// что пошло не так именно в этом файле. Иначе ошибка подстановки оставалась бы
    /// в журнале, а в стандартный вывод шла бы строка с кодом «ОК»: файл с молчаливо
    /// пропущенным значением опаснее отсутствующего.
    /// </remarks>
    IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Возвращает значение для указанного поля.</summary>
    ResolvedValue Resolve(CallNode call, RenderContext context);

    /// <summary>Число точек по оси в текущей вырезке.</summary>
    int AxisLength(Dimensions axis, RenderContext context);

    /// <summary>Значение точки оси в виде текста — для заголовков таблицы.</summary>
    string AxisHeader(Dimensions axis, int index, RenderContext context);

    /// <summary>Значение ячейки таблицы на пересечении колонки и строки.</summary>
    ResolvedValue Cell(
        string function,
        Dimensions columnAxis,
        int column,
        Dimensions rowAxis,
        int row,
        RenderContext context);
}
