using System.Globalization;
using ExportMaster.Core;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Подставляет заглушки вместо значений: файлы данных ещё не читаются.
/// </summary>
/// <remarks>
/// Позволяет прогнать шаблон и увидеть форму будущего файла до появления
/// файлов измерений — а заодно проверить сам шаблон на ошибки.
/// </remarks>
public sealed class StubValueResolver : IValueResolver
{
    private const char Open = '‹';
    private const char Close = '›';

    public StubValueResolver(StubStyle style = StubStyle.Descriptive)
    {
        Style = style;
    }

    public StubStyle Style { get; }

    public ResolvedValue Resolve(CallNode call, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(context);

        if (context.CompactStubs)
        {
            return ResolvedValue.FromText(CallText.Compact(call));
        }

        var description = $"{Open}{CallText.Format(call)}{Close}";

        if (Style == StubStyle.Descriptive)
        {
            return ResolvedValue.FromText(description);
        }

        var number = SampleNumber(call);
        return ResolvedValue.FromNumber(
            number,
            number.ToString("0.####", context.Settings.NumberFormat));
    }

    /// <summary>
    /// Правдоподобное значение в диапазоне −99.99…99.99, устойчиво выведенное из
    /// записи поля.
    /// </summary>
    /// <remarks>
    /// Одинаковые поля дают одинаковые числа при любом запуске: иначе эталонные
    /// выходные файлы было бы не с чем сравнивать. <see cref="string.GetHashCode()"/>
    /// для этого не годится — в .NET он различается от запуска к запуску.
    /// </remarks>
    private static double SampleNumber(CallNode call)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var symbol in CallText.Format(call))
        {
            hash = (hash ^ symbol) * prime;
        }

        // Приведение к int обязательно: константа 10000 неявно становится uint,
        // вычитание пошло бы в беззнаковых числах и завернулось при малых значениях.
        var scaled = (int)(hash % 20000);
        return (scaled - 10000) / 100.0;
    }

    /// <summary>Размер сетки, пока настоящих осей нет.</summary>
    public int AxisLength(Dimensions axis, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _ = axis;
        return context.StubAxisLength;
    }

    /// <summary>Заглушка заголовка оси: имя оси и порядковый номер значения.</summary>
    public string AxisHeader(Dimensions axis, int index, RenderContext context)
    {
        _ = context;

        return Style == StubStyle.Descriptive
            ? $"{Open}{axis}#{index + 1}{Close}"
            : $"{axis}#{(index + 1).ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Заглушка ячейки. Номера строки и колонки входят в её состав, чтобы значения
    /// различались: одинаковые числа по всей таблице скрыли бы сбитые колонки.
    /// </summary>
    public ResolvedValue Cell(
        string function,
        Dimensions columnAxis,
        int column,
        Dimensions rowAxis,
        int row,
        RenderContext context)
    {
        _ = columnAxis;
        _ = rowAxis;

        var cell = new CallNode(
            function,
            [
                new NumberArgument(row + 1, (row + 1).ToString(CultureInfo.InvariantCulture), default),
                new NumberArgument(column + 1, (column + 1).ToString(CultureInfo.InvariantCulture), default),
            ],
            default);

        return Resolve(cell, context);
    }
}
