using System.Globalization;
using ExportMaster.Engine.Jobs;
using ExportMaster.Formats;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Превращает значение поля заголовка в текст для выходного файла.
/// </summary>
/// <remarks>
/// Решения о представлении приняты заказчиком: перечислимые поля выводятся подписью,
/// а не кодом; незаданное значение — словами; дата и время — в виде
/// <c>ДД.ММ.ГГГГ ЧЧ:ММ:СС</c>; к дистанции сбора данных приписывается единица
/// измерения, определённая осью сбора данных. Само чтение полей к представлению
/// отношения не имеет и живёт в <see cref="MeasurementHeader"/>.
/// </remarks>
public sealed class HeaderFieldFormatter
{
    /// <summary>Число знаков по умолчанию: шесть значащих, как и у значений матрицы.</summary>
    private const string NumberFormat = "G6";

    private readonly OutputSettings _settings;

    public HeaderFieldFormatter(OutputSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Значение поля в виде текста; числовые поля вдобавок остаются числами,
    /// чтобы к ним применялось поле <c>FORMAT</c>.
    /// </summary>
    /// <param name="warning">
    /// Замечание для журнала, если значение поля не укладывается в описание формата.
    /// </param>
    public ResolvedValue Format(MeasurementHeader header, HeaderField field, out string? warning)
    {
        ArgumentNullException.ThrowIfNull(header);

        warning = null;
        var value = header.Read(field);

        switch (value.Kind)
        {
            case HeaderValueKind.Unset:
                return ResolvedValue.FromText(_settings.UnsetText);

            case HeaderValueKind.Label:
            case HeaderValueKind.Text:
                return ResolvedValue.FromText(value.Text ?? string.Empty);

            case HeaderValueKind.Number:
                return ResolvedValue.FromNumber(
                    value.Number,
                    value.Number.ToString(NumberFormat, _settings.NumberFormat) + HeaderLabels.Of(value.Unit));

            case HeaderValueKind.Moment:
                return ResolvedValue.FromText(FormatMoment(value.Number));

            default:
                // Код вне перечисления ТЗ выводится числом, но в журнале остаётся след:
                // это может быть и признак сбитой раскладки файла.
                warning = $"Поле заголовка {field}: значение {value.Number} "
                    + "отсутствует в перечислении ТЗ п. 4.2.1.6 и выведено числом.";

                return ResolvedValue.FromNumber(
                    value.Number,
                    value.Number.ToString(NumberFormat, _settings.NumberFormat));
        }
    }

    /// <summary>
    /// Дата и время по секундам от 1.1.1970.
    /// </summary>
    /// <remarks>
    /// Выводится в UTC: местное время сделало бы один и тот же файл данных разным
    /// на разных машинах, а разбирающая сторона у заказчика ждёт что-то одно.
    /// </remarks>
    private string FormatMoment(double unixSeconds) =>
        DateTimeOffset.FromUnixTimeSeconds((long)unixSeconds)
            .UtcDateTime
            .ToString(_settings.DateFormat, CultureInfo.InvariantCulture);
}
