using ExportMaster.Core;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Data;

/// <summary>
/// Вырезка матрицы: оси разбиения зафиксированы, остальные свободны.
/// </summary>
/// <remarks>
/// На каждую вырезку формируется свой выходной файл (ТЗ п. 4.4.6).
/// </remarks>
public sealed class GroupSlice
{
    private readonly int[] _fixedIndices;

    internal GroupSlice(MatrixFile matrix, IReadOnlyList<Dimensions> groupAxes, int[] fixedIndices)
    {
        Matrix = matrix;
        GroupAxes = groupAxes;
        _fixedIndices = fixedIndices;
    }

    public MatrixFile Matrix { get; }

    /// <summary>Оси разбиения в порядке, заданном в шаблоне.</summary>
    public IReadOnlyList<Dimensions> GroupAxes { get; }

    /// <summary>Индекс, на котором зафиксирована ось разбиения; −1 для свободной оси.</summary>
    public int IndexOf(Dimensions axis)
    {
        for (var position = 0; position < GroupAxes.Count; position++)
        {
            if (GroupAxes[position] == axis)
            {
                return _fixedIndices[position];
            }
        }

        return -1;
    }

    /// <summary>Текущее значение оси разбиения в виде текста.</summary>
    public string? ValueOf(Dimensions axis)
    {
        var index = IndexOf(axis);

        if (index < 0)
        {
            return null;
        }

        var position = Matrix.IndexOfAxis(axis);
        return position < 0 ? null : Matrix.Axes[position].TextAt(index);
    }

    /// <summary>
    /// Ключ результирующего файла: значения осей разбиения (ТЗ п. 4.3.3, поле 3).
    /// </summary>
    public string Key => string.Join(';', GroupAxes.Select(axis => $"{axis}={ValueOf(axis)}"));

    /// <summary>
    /// Полный набор индексов для обращения к матрице: оси разбиения берутся из
    /// вырезки, свободные — из переданных значений, остальные равны нулю.
    /// </summary>
    internal int[] BuildIndices(IReadOnlyList<(Dimensions Axis, int Index)> free)
    {
        var indices = new int[Matrix.Axes.Count];

        for (var axis = 0; axis < Matrix.Axes.Count; axis++)
        {
            var fixedIndex = IndexOf(Matrix.Axes[axis].Type);
            indices[axis] = fixedIndex >= 0 ? fixedIndex : 0;
        }

        foreach (var (axis, index) in free)
        {
            var position = Matrix.IndexOfAxis(axis);

            if (position >= 0)
            {
                indices[position] = index;
            }
        }

        return indices;
    }
}
