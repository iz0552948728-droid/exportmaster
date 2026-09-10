using System.Numerics;

namespace ExportMaster.Engine.Data;

/// <summary>
/// Величины, извлекаемые из комплексного значения.
/// </summary>
/// <remarks>
/// Состав будет задан перечислением на стороне заказчика разработки. Пока здесь
/// только то, что встречается в примерах ТЗ, плюс очевидные соседи.
/// </remarks>
public static class QuantityFunctions
{
    /// <summary>Известно ли имя величины.</summary>
    public static bool IsKnown(string name) => name is "AMP" or "MAG" or "PHASE" or "RE" or "IM";

    /// <summary>Вычисляет величину; для неизвестного имени возвращает <c>null</c>.</summary>
    public static double? Evaluate(string name, Complex value) => name switch
    {
        // В примере ТЗ п. 4.4.3.3 результат AMP подписан «дБ», поэтому логарифм
        // входит в саму функцию. Линейный модуль доступен отдельно как MAG.
        "AMP" => 20 * Math.Log10(value.Magnitude),
        "MAG" => value.Magnitude,
        "PHASE" => value.Phase * 180 / Math.PI,
        "RE" => value.Real,
        "IM" => value.Imaginary,
        _ => null,
    };
}
