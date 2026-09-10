namespace ExportMaster.Formats;

/// <summary>Вид значений в блоке данных (ТЗ п. 4.2.1.2, поле 1).</summary>
public enum DataNature : byte
{
    /// <summary>Пара <see cref="double"/> — действительная и мнимая части.</summary>
    Complex = 0,

    /// <summary>Одно значение <see cref="double"/>.</summary>
    Real = 1,
}

/// <summary>Порядок байт содержимого файла (ТЗ п. 4.2.1.2, поле 2).</summary>
public enum ByteOrderKind : byte
{
    BigEndian = 1,
    LittleEndian = 2,
}

/// <summary>Способ задания значений оси (ТЗ п. 4.2.1.2, поле 3.3).</summary>
public enum AxisValueSource : byte
{
    /// <summary>Хранятся начальное значение, конечное и шаг.</summary>
    Range = 0,

    /// <summary>Хранятся все значения подряд.</summary>
    List = 1,
}

/// <summary>Тип хранимых значений оси (ТЗ п. 4.2.1.2, поле 3.4).</summary>
public enum AxisValueKind : byte
{
    /// <summary>Значения не хранятся; точки нумеруются подряд.</summary>
    Standard = 0,

    Strings = 1,

    Numbers = 2,
}
