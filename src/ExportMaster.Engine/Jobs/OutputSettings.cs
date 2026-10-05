using System.Globalization;
using System.Text;
using ExportMaster.Core;

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

    /// <summary>
    /// Число знаков после запятой для значений отдельных осей, заданное директивами
    /// <c>AXISFORMAT</c>. Ось, которой здесь нет, выводится так, как записана в файле.
    /// </summary>
    public IReadOnlyDictionary<Dimensions, string> AxisFormats { get; init; } =
        new Dictionary<Dimensions, string>();

    /// <summary>
    /// Вид даты и времени в полях заголовка. По умолчанию ДД.ММ.ГГГГ ЧЧ:ММ:СС,
    /// как условлено с заказчиком; переключается директивой <c>DATEFORMAT</c>.
    /// </summary>
    public string DateFormat { get; init; } = "dd.MM.yyyy HH:mm:ss";

    /// <summary>
    /// Текст на месте незаданного значения. В файле это 255, −1 или NaN, смотря
    /// по полю, а в отчёте — одно и то же слово.
    /// </summary>
    public string UnsetText { get; init; } = "не задано";

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
