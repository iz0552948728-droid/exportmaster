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

    /// <summary>
    /// Значение точки оси в виде текста.
    /// </summary>
    /// <param name="withUnit">
    /// Приписывать ли подпись единицы к числу. В заголовках таблицы она не нужна —
    /// там единица стоит один раз, в угловой ячейке, — а в имени файла и в ключе
    /// результирующего файла нужна: иначе <c>meas_20.000.txt</c> не говорит ни о чём.
    /// </param>
    public string Format(AxisDescriptor axis, int index, bool withUnit = true)
    {
        ArgumentNullException.ThrowIfNull(axis);

        if (axis.ValueKind != AxisValueKind.Numbers)
        {
            return axis.TextAt(index);
        }

        var value = axis.NumberAt(index);

        if (axis.Type == Dimensions.FREQ)
        {
            var format = _formats.GetValueOrDefault(axis.Type, DefaultFrequencyFormat);
            var text = (value / FrequencyScale(axis)).ToString(format, _numberFormat);

            return withUnit ? text + FrequencyUnit(axis) : text;
        }

        return _formats.TryGetValue(axis.Type, out var axisFormat)
            ? value.ToString(axisFormat, _numberFormat)
            : axis.TextAt(index);
    }

    /// <summary>
    /// Подпись единицы частоты для всей оси: гигагерцы, а ниже одного гигагерца
    /// мегагерцы.
    /// </summary>
    /// <remarks>
    /// Приставка выбирается один раз на ось, а не на каждое значение: столбец, где
    /// часть заголовков в мегагерцах, а часть в гигагерцах, читать невозможно.
    /// </remarks>
    public static string FrequencyUnit(AxisDescriptor axis) =>
        FrequencyScale(axis) >= 1e9 ? "ГГц" : "МГц";

    /// <summary>Делитель значений оси частоты: один на всю ось, по наибольшему значению.</summary>
    private static double FrequencyScale(AxisDescriptor axis)
    {
        var highest = 0d;

        for (var index = 0; index < axis.Length; index++)
        {
            highest = Math.Max(highest, Math.Abs(axis.NumberAt(index)));
        }

        return highest >= 1e9 ? 1e9 : 1e6;
    }
}
