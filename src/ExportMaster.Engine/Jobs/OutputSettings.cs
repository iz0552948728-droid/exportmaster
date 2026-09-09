using System.Globalization;
using System.Text;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Настройки выходного файла, задаваемые директивами заголовка шаблона.
/// </summary>
public sealed class OutputSettings
{
    /// <summary>
    /// Десятичный разделитель. По умолчанию точка и намеренно не берётся из локали
    /// системы: иначе одно задание на двух машинах дало бы разные файлы, а
    /// разбирающая сторона у заказчика ждёт что-то одно.
    /// </summary>
    public string DecimalSeparator { get; init; } = ".";

    /// <summary>Перевод строки. По умолчанию — принятый в операционной системе.</summary>
    public string NewLine { get; init; } = Environment.NewLine;

    /// <summary>Кодировка выходного файла. По умолчанию UTF-8 без BOM.</summary>
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Формат чисел с выбранным десятичным разделителем.</summary>
    public NumberFormatInfo NumberFormat
    {
        get
        {
            var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            format.NumberDecimalSeparator = DecimalSeparator;
            return format;
        }
    }
}
