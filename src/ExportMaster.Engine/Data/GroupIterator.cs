using ExportMaster.Core;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Data;

/// <summary>
/// Перебирает вырезки матрицы по осям разбиения.
/// </summary>
public static class GroupIterator
{
    /// <summary>
    /// Перебирает все комбинации значений осей разбиения. Первая ось списка
    /// внешняя, последняя меняется чаще всего — порядок задаёт шаблон.
    /// </summary>
    /// <remarks>
    /// Число осей не ограничено: разбиение работает при любой размерности матрицы.
    /// Ось с единственным значением даёт одну группу и на устройство перебора
    /// не влияет.
    /// </remarks>
    public static IEnumerable<GroupSlice> Enumerate(MatrixFile matrix, IReadOnlyList<Dimensions> groupAxes)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(groupAxes);

        var lengths = new int[groupAxes.Count];

        for (var position = 0; position < groupAxes.Count; position++)
        {
            var axis = matrix.IndexOfAxis(groupAxes[position]);

            if (axis < 0)
            {
                throw new MatrixFormatException($"Ось разбиения {groupAxes[position]} в файле данных отсутствует.");
            }

            lengths[position] = matrix.Axes[axis].Length;
        }

        var indices = new int[groupAxes.Count];

        while (true)
        {
            yield return new GroupSlice(matrix, groupAxes, (int[])indices.Clone());

            // Перенос разрядов справа налево: последняя ось списка меняется чаще всего.
            var position = groupAxes.Count - 1;

            while (position >= 0)
            {
                indices[position]++;

                if (indices[position] < lengths[position])
                {
                    break;
                }

                indices[position] = 0;
                position--;
            }

            if (position < 0)
            {
                yield break;
            }
        }
    }

    /// <summary>Сколько файлов даст разбиение.</summary>
    public static int Count(MatrixFile matrix, IReadOnlyList<Dimensions> groupAxes)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(groupAxes);

        var total = 1;

        foreach (var groupAxis in groupAxes)
        {
            var axis = matrix.IndexOfAxis(groupAxis);
            total *= axis < 0 ? 0 : matrix.Axes[axis].Length;
        }

        return total;
    }
}
