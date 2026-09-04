using System.Globalization;
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

    /// <summary>Заглушка заголовка оси: имя оси и порядковый номер значения.</summary>
    internal string AxisHeader(string axis, int index, RenderContext context)
    {
        _ = context;

        return Style == StubStyle.Descriptive
            ? $"{Open}{axis}#{index}{Close}"
            : $"{axis}#{index.ToString(CultureInfo.InvariantCulture)}";
    }
}
