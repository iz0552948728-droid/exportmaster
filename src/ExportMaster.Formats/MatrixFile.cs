using System.Numerics;
using ExportMaster.Core;

namespace ExportMaster.Formats;

/// <summary>
/// Многомерная матрица результатов измерений (сигнатура 1).
/// </summary>
public sealed class MatrixFile
{
    private readonly double[] _data;
    private readonly int[] _strides;

    internal MatrixFile(
        DataNature nature,
        IReadOnlyList<AxisDescriptor> axes,
        double[] data,
        MeasurementHeader header)
    {
        Nature = nature;
        Axes = axes;
        Header = header;
        _data = data;

        // Последний описатель — ось внутри одномерного массива, поэтому по нему шаг
        // единичный, а каждый предыдущий шагает через всё, что лежит правее (ТЗ п. 4.2.1.4).
        _strides = new int[axes.Count];
        var stride = 1;
        for (var index = axes.Count - 1; index >= 0; index--)
        {
            _strides[index] = stride;
            stride *= axes[index].Length;
        }

        PointCount = stride;
    }

    /// <summary>Вид значений: комплексные или вещественные.</summary>
    public DataNature Nature { get; }

    /// <summary>Описатели измерений в порядке следования в файле.</summary>
    public IReadOnlyList<AxisDescriptor> Axes { get; }

    /// <summary>Блок данных заголовка: условия измерения (ТЗ п. 4.2.1.6).</summary>
    public MeasurementHeader Header { get; }

    /// <summary>Число точек во всей матрице.</summary>
    public int PointCount { get; }

    /// <summary>Число значений <see cref="double"/> на точку: 1 или 2.</summary>
    public int ValuesPerPoint => Nature == DataNature.Complex ? 2 : 1;

    /// <summary>
    /// Единица измерения значений оси.
    /// </summary>
    /// <remarks>
    /// Берётся из описателя оси. У файлов редакций до 1.4 описатель её не несёт,
    /// и для оси сбора данных она выводится из поля <c>DataAxis</c> заголовка —
    /// так эти файлы продолжают читаться без потери подписи.
    /// </remarks>
    public MeasureUnit? UnitOf(Dimensions axis)
    {
        var position = IndexOfAxis(axis);

        if (position < 0)
        {
            return null;
        }

        if (Axes[position].Unit is { } declared)
        {
            return declared;
        }

        return axis == Dimensions.DATA
            ? Header.DataAxisUnit switch
            {
                DistanceUnit.Degrees => MeasureUnit.Degrees,
                DistanceUnit.Meters => MeasureUnit.Meters,
                _ => null,
            }
            : null;
    }

    /// <summary>Ищет описатель по типу оси; возвращает −1, если такой оси в файле нет.</summary>
    public int IndexOfAxis(Dimensions axis)
    {
        for (var index = 0; index < Axes.Count; index++)
        {
            if (Axes[index].Type == axis)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Значение в точке; для вещественных данных мнимая часть нулевая.</summary>
    public Complex ValueAt(ReadOnlySpan<int> indices)
    {
        var offset = Offset(indices) * ValuesPerPoint;

        return Nature == DataNature.Complex
            ? new Complex(_data[offset], _data[offset + 1])
            : new Complex(_data[offset], 0);
    }

    private int Offset(ReadOnlySpan<int> indices)
    {
        if (indices.Length != Axes.Count)
        {
            throw new ArgumentException(
                $"Матрица имеет {Axes.Count} измерений, а индексов передано {indices.Length}.",
                nameof(indices));
        }

        var offset = 0;

        for (var axis = 0; axis < indices.Length; axis++)
        {
            var index = indices[axis];

            if (index < 0 || index >= Axes[axis].Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(indices),
                    index,
                    $"Ось {Axes[axis].Type} содержит {Axes[axis].Length} точек.");
            }

            offset += index * _strides[axis];
        }

        return offset;
    }
}
