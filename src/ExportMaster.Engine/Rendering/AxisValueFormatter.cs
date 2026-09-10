using System.Globalization;
using ExportMaster.Core;
using ExportMaster.Engine.Jobs;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Превращает значение точки оси в текст для заголовков таблицы, имени файла
/// и ключа результирующего файла.
/// </summary>
/// <remarks>
/// Частота хранится в герцах, а читается человеком в гигагерцах: 2725000000
/// вместо 2.725 ГГц нечитаемо и в заголовке таблицы, и в имени файла. Поэтому
/// единица выбирается по величине, а подпись приписывается к числу.
/// </remarks>
public sealed class AxisValueFormatter
{
    /// <summary>Число знаков после запятой по умолчанию.</summary>
    public const string DefaultFrequencyFormat = "0.000";

    private readonly IReadOnlyDictionary<Dimensions, string> _formats;
    private readonly NumberFormatInfo _numberFormat;

    public AxisValueFormatter(OutputSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _formats = settings.AxisFormats;
        _numberFormat = settings.NumberFormat;
    }

    /// <summary>Значение точки оси в виде текста.</summary>
    public string Format(AxisDescriptor axis, int index)
    {
        ArgumentNullException.ThrowIfNull(axis);

        if (axis.ValueKind != AxisValueKind.Numbers)
        {
            return axis.TextAt(index);
        }

        var value = axis.NumberAt(index);

        if (axis.Type == Dimensions.FREQ)
        {
            return FormatFrequency(value, _formats.GetValueOrDefault(axis.Type, DefaultFrequencyFormat));
        }

        return _formats.TryGetValue(axis.Type, out var format)
            ? value.ToString(format, _numberFormat)
            : axis.TextAt(index);
    }

    /// <summary>
    /// Частота с подписью: гигагерцы, а ниже одного гигагерца — мегагерцы.
    /// </summary>
    /// <remarks>
    /// Подпись пишется вплотную к числу: значение попадает и в имя файла,
    /// а пробелы в именах неудобны.
    /// </remarks>
    private string FormatFrequency(double hertz, string format) =>
        Math.Abs(hertz) >= 1e9
            ? (hertz / 1e9).ToString(format, _numberFormat) + "GHz"
            : (hertz / 1e6).ToString(format, _numberFormat) + "MHz";
}
