using System.Globalization;
using ExportMaster.Core;

namespace ExportMaster.Formats;

/// <summary>
/// Описатель одного измерения матрицы (ТЗ п. 4.2.1.2, поле 3).
/// </summary>
public sealed class AxisDescriptor
{
    private readonly double[]? _numbers;
    private readonly string[]? _strings;
    private readonly double _start;
    private readonly double _step;

    private AxisDescriptor(
        Dimensions type,
        int length,
        AxisValueSource source,
        AxisValueKind valueKind,
        double[]? numbers,
        string[]? strings,
        double start,
        double step)
    {
        Type = type;
        Length = length;
        Source = source;
        ValueKind = valueKind;
        _numbers = numbers;
        _strings = strings;
        _start = start;
        _step = step;
    }

    /// <summary>Тип значения: какая это ось.</summary>
    public Dimensions Type { get; }

    /// <summary>
    /// Число точек по оси.
    /// </summary>
    /// <remarks>
    /// Не совпадает с полем ValueCount из файла: для диапазона там записано число
    /// хранимых значений, то есть три — начало, конец и шаг, — а точек может быть
    /// сколько угодно.
    /// </remarks>
    public int Length { get; }

    public AxisValueSource Source { get; }

    public AxisValueKind ValueKind { get; }

    /// <summary>Ось, заданная диапазоном: начало, конец и шаг.</summary>
    public static AxisDescriptor FromRange(Dimensions type, double start, double end, double step)
    {
        if (step == 0)
        {
            throw new MatrixFormatException($"Ось {type}: шаг диапазона равен нулю.");
        }

        var length = (int)Math.Round((end - start) / step) + 1;

        if (length <= 0)
        {
            throw new MatrixFormatException(
                $"Ось {type}: диапазон {start} … {end} с шагом {step} не содержит ни одной точки.");
        }

        return new AxisDescriptor(type, length, AxisValueSource.Range, AxisValueKind.Numbers, null, null, start, step);
    }

    /// <summary>Ось, заданная списком чисел.</summary>
    public static AxisDescriptor FromNumbers(Dimensions type, double[] values) =>
        new(type, values.Length, AxisValueSource.List, AxisValueKind.Numbers, values, null, 0, 0);

    /// <summary>Ось, заданная списком строк: имена лучей, портов и тому подобное.</summary>
    public static AxisDescriptor FromStrings(Dimensions type, string[] values) =>
        new(type, values.Length, AxisValueSource.List, AxisValueKind.Strings, null, values, 0, 0);

    /// <summary>Ось без хранимых значений: точки просто нумеруются.</summary>
    public static AxisDescriptor FromStandardList(Dimensions type, int length) =>
        new(type, length, AxisValueSource.List, AxisValueKind.Standard, null, null, 0, 0);

    /// <summary>Числовое значение точки; для строковых осей недоступно.</summary>
    public double NumberAt(int index)
    {
        CheckIndex(index);

        if (Source == AxisValueSource.Range)
        {
            return _start + (_step * index);
        }

        return _numbers is not null
            ? _numbers[index]
            : throw new MatrixFormatException($"Ось {Type} не содержит числовых значений.");
    }

    /// <summary>Значение точки в виде текста — для заголовков таблицы и ключа файла.</summary>
    public string TextAt(int index)
    {
        CheckIndex(index);

        if (_strings is not null)
        {
            return _strings[index];
        }

        if (ValueKind == AxisValueKind.Standard)
        {
            return index.ToString(CultureInfo.InvariantCulture);
        }

        // Двенадцати значащих цифр хватает любому измерению, а пятнадцать
        // выносят наружу накопленную ошибку: -1.3 + 0.05*24 даёт -0.0999999999999999.
        return NumberAt(index).ToString("G12", CultureInfo.InvariantCulture);
    }

    private void CheckIndex(int index)
    {
        if (index < 0 || index >= Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"Ось {Type} содержит {Length} точек.");
        }
    }
}
