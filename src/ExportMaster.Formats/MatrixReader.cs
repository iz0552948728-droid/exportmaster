using System.Buffers.Binary;
using System.Text;
using ExportMaster.Core;

namespace ExportMaster.Formats;

/// <summary>
/// Читает файл многомерной матрицы (<c>.mtx</c>).
/// </summary>
/// <remarks>
/// Раскладка файла подтверждена на образце от автора формата; отличия от ТЗ ред. 1.2
/// перечислены в <c>docs/format-spec.md</c>. Существенные из них: размер метаданных
/// отсчитывается от начала файла и указывает прямо на матрицу, блок данных заголовка
/// лежит между метаданными и описателями структуры, а поле ValueCount при задании оси
/// диапазоном означает число хранимых значений, а не число точек.
/// </remarks>
public static class MatrixReader
{
    /// <summary>Сигнатура файла многомерной матрицы.</summary>
    public const uint MatrixSignature = 1;

    private const int HeaderSize = 8;

    public static MatrixFile Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            return Read(File.ReadAllBytes(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new MatrixFormatException($"Не удалось прочитать файл данных: {exception.Message}", exception);
        }
    }

    public static MatrixFile Read(ReadOnlySpan<byte> content)
    {
        if (content.Length < HeaderSize)
        {
            throw new MatrixFormatException("Файл короче заголовка.");
        }

        // Шапка файла читается как little endian всегда: поле, задающее порядок байт,
        // лежит внутри метаданных и до их чтения недоступно.
        var signature = BinaryPrimitives.ReadUInt32LittleEndian(content);

        if (signature != MatrixSignature)
        {
            throw new MatrixFormatException(
                $"Сигнатура {signature} не соответствует многомерной матрице (ожидалась {MatrixSignature}).");
        }

        var metadataSize = BinaryPrimitives.ReadUInt32LittleEndian(content[4..]);

        // Размер отсчитывается от начала файла: это смещение, с которого идут данные.
        if (metadataSize < HeaderSize || metadataSize > content.Length)
        {
            throw new MatrixFormatException(
                $"Размер метаданных {metadataSize} выходит за пределы файла длиной {content.Length}.");
        }

        var cursor = new Cursor(content, HeaderSize);

        var nature = (DataNature)cursor.ReadByte();

        if (!Enum.IsDefined(nature))
        {
            throw new MatrixFormatException($"Неизвестный вид блока данных: {(byte)nature}.");
        }

        var order = (ByteOrderKind)cursor.ReadByte();

        if (!Enum.IsDefined(order))
        {
            throw new MatrixFormatException($"Неизвестный порядок байт: {(byte)order}.");
        }

        cursor.LittleEndian = order == ByteOrderKind.LittleEndian;

        var blockCount = cursor.ReadUInt32();

        if (blockCount == 0)
        {
            throw new MatrixFormatException("Матрица не содержит ни одного описателя измерений.");
        }

        var stringBlockSize = cursor.ReadUInt32();

        if (stringBlockSize > content.Length)
        {
            throw new MatrixFormatException(
                $"Размер блока строк заголовка {stringBlockSize} больше всего файла ({content.Length} байт).");
        }

        // Блок данных заголовка в файле лежит раньше описателей структуры, хотя ТЗ
        // п. 4.2.1.4 относит его к блоку данных. Порядок взят из образца.
        var header = ReadHeader(ref cursor, (int)stringBlockSize);

        var axes = new AxisDescriptor[blockCount];

        for (var index = 0; index < blockCount; index++)
        {
            axes[index] = ReadAxis(ref cursor);
        }

        if (cursor.Position != metadataSize)
        {
            throw new MatrixFormatException(
                $"Метаданные разобраны до смещения {cursor.Position}, а по заголовку их размер {metadataSize}.");
        }

        var matrix = ReadData(content, (int)metadataSize, nature, axes, order, header);
        return matrix;
    }

    /// <summary>
    /// Читает блок данных заголовка: 115 байт полей и следом блок строк
    /// (ТЗ пп. 4.2.1.5—4.2.1.7).
    /// </summary>
    private static MeasurementHeader ReadHeader(ref Cursor cursor, int stringBlockSize)
    {
        var start = cursor.Position;

        var header = new MeasurementHeader
        {
            MeasurementTypeCode = cursor.ReadByte(),
            CoordinateSystemCode = cursor.ReadByte(),
            ChamberTypeCode = cursor.ReadByte(),
            EquivalentDistance = cursor.ReadSingle(),
            DateStart = cursor.ReadUInt64(),
            DateEnd = cursor.ReadUInt64(),
            AuxPolarizationCode = cursor.ReadByte(),
            AuxType = cursor.ReadInt32(),
            AuxIdentifier = cursor.ReadInt32(),
            Distance = cursor.ReadSingle(),
            IFBW = cursor.ReadSingle(),
            P = cursor.ReadSingle(),
            DataAxisCode = cursor.ReadByte(),
            MeasurementDistance = cursor.ReadSingle(),
            ReferencePortBalancing = cursor.ReadByte(),
            ReferenceChanelBalancing = cursor.ReadByte(),
            SyncMode = cursor.ReadByte(),
            SyncModeTypeCode = cursor.ReadByte(),
            T1 = cursor.ReadUInt32(),
            T2 = cursor.ReadUInt32(),
            T3 = cursor.ReadUInt32(),
            T4 = cursor.ReadUInt32(),
            T5 = cursor.ReadUInt32(),
            SW1 = cursor.ReadInt32(),
            SW2 = cursor.ReadInt32(),
            SW3 = cursor.ReadInt32(),
            ChanelStatic = cursor.ReadByte(),
            PolarizationStatic = cursor.ReadByte(),
            ZeroX = cursor.ReadSingle(),
            ZeroY = cursor.ReadSingle(),
            ZeroZ = cursor.ReadSingle(),
            ZeroP = cursor.ReadSingle(),
            ZeroR = cursor.ReadSingle(),
            ZeroAz = cursor.ReadSingle(),
            ZeroEl = cursor.ReadSingle(),
            Strings = cursor.ReadStringBlock(stringBlockSize),
        };

        var read = cursor.Position - start;
        var expected = MeasurementHeader.FixedBlockSize + stringBlockSize;

        // Сверка с объявленным размером: если состав полей разойдётся с ТЗ,
        // ошибка должна проявиться здесь, а не превратиться в сдвиг описателей.
        if (read != expected)
        {
            throw new MatrixFormatException(
                $"Блок данных заголовка разобран на {read} байт вместо {expected}.");
        }

        return header;
    }

    private static AxisDescriptor ReadAxis(ref Cursor cursor)
    {
        var rawType = cursor.ReadByte();

        if (!Enum.IsDefined((Dimensions)rawType))
        {
            throw new MatrixFormatException(
                $"Неизвестный тип значения {rawType}. Допустимы 1…8, см. перечисление Dimensions.");
        }

        var type = (Dimensions)rawType;
        var valueCount = cursor.ReadUInt16();
        var source = (AxisValueSource)cursor.ReadByte();
        var valueKind = (AxisValueKind)cursor.ReadByte();

        if (!Enum.IsDefined(source))
        {
            throw new MatrixFormatException($"Ось {type}: неизвестный способ задания списка значений.");
        }

        if (!Enum.IsDefined(valueKind))
        {
            throw new MatrixFormatException($"Ось {type}: неизвестный тип данных списка значений.");
        }

        if (valueKind == AxisValueKind.Standard)
        {
            return AxisDescriptor.FromStandardList(type, valueCount);
        }

        if (source == AxisValueSource.Range)
        {
            if (valueKind != AxisValueKind.Numbers)
            {
                throw new MatrixFormatException($"Ось {type}: диапазон задаётся только числами.");
            }

            // Для диапазона хранятся ровно три значения: начало, конец и шаг.
            return AxisDescriptor.FromRange(type, cursor.ReadDouble(), cursor.ReadDouble(), cursor.ReadDouble());
        }

        if (valueKind == AxisValueKind.Strings)
        {
            var strings = new string[valueCount];

            for (var index = 0; index < valueCount; index++)
            {
                strings[index] = cursor.ReadNullTerminatedString();
            }

            return AxisDescriptor.FromStrings(type, strings);
        }

        var numbers = new double[valueCount];

        for (var index = 0; index < valueCount; index++)
        {
            numbers[index] = cursor.ReadDouble();
        }

        return AxisDescriptor.FromNumbers(type, numbers);
    }

    private static MatrixFile ReadData(
        ReadOnlySpan<byte> content,
        int offset,
        DataNature nature,
        AxisDescriptor[] axes,
        ByteOrderKind order,
        MeasurementHeader header)
    {
        var points = 1L;

        foreach (var axis in axes)
        {
            points *= axis.Length;
        }

        var valuesPerPoint = nature == DataNature.Complex ? 2 : 1;
        var expected = points * valuesPerPoint * sizeof(double);
        var available = content.Length - offset;

        if (expected != available)
        {
            throw new MatrixFormatException(
                $"Размер блока данных {available} байт не соответствует описанию: "
                    + $"{points} точек по {valuesPerPoint * sizeof(double)} байт дают {expected}.");
        }

        var data = new double[points * valuesPerPoint];
        var cursor = new Cursor(content, offset) { LittleEndian = order == ByteOrderKind.LittleEndian };

        for (var index = 0; index < data.Length; index++)
        {
            data[index] = cursor.ReadDouble();
        }

        return new MatrixFile(nature, axes, data, header);
    }

    /// <summary>Последовательное чтение с учётом порядка байт.</summary>
    private ref struct Cursor
    {
        private readonly ReadOnlySpan<byte> _content;

        public Cursor(ReadOnlySpan<byte> content, int position)
        {
            _content = content;
            Position = position;
            LittleEndian = true;
        }

        public int Position { get; private set; }

        public bool LittleEndian { get; set; }

        public byte ReadByte()
        {
            Require(1);
            return _content[Position++];
        }

        public ushort ReadUInt16()
        {
            Require(2);
            var slice = _content.Slice(Position, 2);
            Position += 2;

            return LittleEndian
                ? BinaryPrimitives.ReadUInt16LittleEndian(slice)
                : BinaryPrimitives.ReadUInt16BigEndian(slice);
        }

        public uint ReadUInt32()
        {
            Require(4);
            var slice = _content.Slice(Position, 4);
            Position += 4;

            return LittleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(slice)
                : BinaryPrimitives.ReadUInt32BigEndian(slice);
        }

        public int ReadInt32() => (int)ReadUInt32();

        public ulong ReadUInt64()
        {
            Require(8);
            var slice = _content.Slice(Position, 8);
            Position += 8;

            return LittleEndian
                ? BinaryPrimitives.ReadUInt64LittleEndian(slice)
                : BinaryPrimitives.ReadUInt64BigEndian(slice);
        }

        public float ReadSingle()
        {
            Require(4);
            var slice = _content.Slice(Position, 4);
            Position += 4;

            return LittleEndian
                ? BinaryPrimitives.ReadSingleLittleEndian(slice)
                : BinaryPrimitives.ReadSingleBigEndian(slice);
        }

        public double ReadDouble()
        {
            Require(8);
            var slice = _content.Slice(Position, 8);
            Position += 8;

            return LittleEndian
                ? BinaryPrimitives.ReadDoubleLittleEndian(slice)
                : BinaryPrimitives.ReadDoubleBigEndian(slice);
        }

        public string ReadNullTerminatedString()
        {
            var start = Position;

            while (Position < _content.Length && _content[Position] != 0)
            {
                Position++;
            }

            if (Position >= _content.Length)
            {
                throw new MatrixFormatException("Строка в метаданных не завершена нулём.");
            }

            var value = Encoding.UTF8.GetString(_content[start..Position]);
            Position++;
            return value;
        }

        /// <summary>
        /// Блок строк заголовка: строки в UTF-8, завершённые нулём, внутри участка
        /// заданного размера. Нумерация строк — порядковая, с нуля (ТЗ п. 4.2.1.7).
        /// </summary>
        public string[] ReadStringBlock(int size)
        {
            Require(size);
            var block = _content.Slice(Position, size);
            Position += size;

            var strings = new List<string>();
            var start = 0;

            for (var index = 0; index < block.Length; index++)
            {
                if (block[index] != 0)
                {
                    continue;
                }

                strings.Add(Encoding.UTF8.GetString(block[start..index]));
                start = index + 1;
            }

            // Хвост без завершающего нуля означает обрезанный блок: такую строку
            // лучше потерять громко, чем подставить в отчёт обрубок.
            if (start != block.Length)
            {
                throw new MatrixFormatException(
                    "Последняя строка блока строк заголовка не завершена нулём.");
            }

            return [.. strings];
        }

        private void Require(int count)
        {
            if (Position + count > _content.Length)
            {
                throw new MatrixFormatException("Файл данных обрывается посреди метаданных.");
            }
        }
    }
}
