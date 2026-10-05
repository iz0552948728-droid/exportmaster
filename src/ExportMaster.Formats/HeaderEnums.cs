namespace ExportMaster.Formats;

/// <summary>Вид измерения (ТЗ п. 4.2.1.6, поле 1).</summary>
public enum MeasurementKind : byte
{
    /// <summary>БЗП.</summary>
    PlanarNearField = 1,

    /// <summary>Сечение.</summary>
    Cut = 2,

    /// <summary>Объемная ДН.</summary>
    VolumePattern = 3,

    /// <summary>Отношение в одной точке.</summary>
    RatioAtPoint = 4,

    /// <summary>Мощность в одной точке.</summary>
    PowerAtPoint = 5,

    /// <summary>Неопределено.</summary>
    Undefined = 255,
}

/// <summary>Система координат (ТЗ п. 4.2.1.6, поле 2).</summary>
public enum CoordinateSystemKind : byte
{
    /// <summary>XY.</summary>
    XY = 1,

    /// <summary>АзЭл.</summary>
    AzEl = 2,

    /// <summary>ЭлАз.</summary>
    ElAz = 3,

    /// <summary>ТэтаФи.</summary>
    ThetaPhi = 4,

    /// <summary>Косинусы.</summary>
    Cosines = 5,

    /// <summary>Вращение по поляризации.</summary>
    PolarizationRotation = 6,

    /// <summary>Неопределено.</summary>
    Undefined = 255,
}

/// <summary>Тип камеры (ТЗ п. 4.2.1.6, поле 3).</summary>
public enum ChamberKind : byte
{
    /// <summary>БЗ.</summary>
    NearField = 1,

    /// <summary>ДЗ.</summary>
    FarField = 2,

    /// <summary>Коллиматор.</summary>
    Collimator = 3,

    /// <summary>Неопределено.</summary>
    Undefined = 255,
}

/// <summary>Поляризация измерительной антенны (ТЗ п. 4.2.1.6, поле 7).</summary>
public enum AuxPolarizationKind : byte
{
    /// <summary>Не задано.</summary>
    Unset = 0,

    /// <summary>H.</summary>
    Horizontal = 1,

    /// <summary>V.</summary>
    Vertical = 2,
}

/// <summary>Ось сбора данных (ТЗ п. 4.2.1.6, поле 13).</summary>
public enum DataAxisKind : byte
{
    /// <summary>X.</summary>
    X = 1,

    /// <summary>Y.</summary>
    Y = 2,

    /// <summary>Az.</summary>
    Az = 3,

    /// <summary>El.</summary>
    El = 4,

    /// <summary>P.</summary>
    P = 5,

    /// <summary>Тэта.</summary>
    Theta = 6,

    /// <summary>Фи.</summary>
    Phi = 7,
}

/// <summary>Вид режима синхронизации (ТЗ п. 4.2.1.6, поле 18).</summary>
public enum SyncModeKind : byte
{
    /// <summary>Простой.</summary>
    Simple = 1,

    /// <summary>Многолучевой.</summary>
    MultiBeam = 2,

    /// <summary>Многоканальный.</summary>
    MultiChannel = 3,

    /// <summary>2П.</summary>
    DualPolarization = 4,
}

/// <summary>
/// Единица измерения дистанции сбора данных.
/// </summary>
/// <remarks>
/// Поле MeasurementDistance задано в градусах или в метрах, и ТЗ не говорит, в чём
/// именно: выбор определяется осью сбора данных. Угловые оси дают градусы,
/// линейные — метры (решение согласовано с автором ТЗ).
/// </remarks>
public enum DistanceUnit : byte
{
    /// <summary>Единица неизвестна либо неприменима.</summary>
    None = 0,

    /// <summary>Градусы.</summary>
    Degrees = 1,

    /// <summary>Метры.</summary>
    Meters = 2,
}

/// <summary>Подписи значений перечислимых полей заголовка — ровно как в ТЗ п. 4.2.1.6.</summary>
public static class HeaderLabels
{
    public static string Of(MeasurementKind value) => value switch
    {
        MeasurementKind.PlanarNearField => "БЗП",
        MeasurementKind.Cut => "Сечение",
        MeasurementKind.VolumePattern => "Объемная ДН",
        MeasurementKind.RatioAtPoint => "Отношение в одной точке",
        MeasurementKind.PowerAtPoint => "Мощность в одной точке",
        _ => "неопределено",
    };

    public static string Of(CoordinateSystemKind value) => value switch
    {
        CoordinateSystemKind.XY => "XY",
        CoordinateSystemKind.AzEl => "АзЭл",
        CoordinateSystemKind.ElAz => "ЭлАз",
        CoordinateSystemKind.ThetaPhi => "ТэтаФи",
        CoordinateSystemKind.Cosines => "Косинусы",
        CoordinateSystemKind.PolarizationRotation => "Вращение по поляризации",
        _ => "неопределено",
    };

    public static string Of(ChamberKind value) => value switch
    {
        ChamberKind.NearField => "БЗ",
        ChamberKind.FarField => "ДЗ",
        ChamberKind.Collimator => "Коллиматор",
        _ => "неопределено",
    };

    public static string Of(AuxPolarizationKind value) => value switch
    {
        AuxPolarizationKind.Horizontal => "H",
        AuxPolarizationKind.Vertical => "V",
        _ => "не задано",
    };

    public static string Of(DataAxisKind value) => value switch
    {
        DataAxisKind.X => "X",
        DataAxisKind.Y => "Y",
        DataAxisKind.Az => "Az",
        DataAxisKind.El => "El",
        DataAxisKind.P => "P",
        DataAxisKind.Theta => "Тэта",
        DataAxisKind.Phi => "Фи",
        _ => "неопределено",
    };

    public static string Of(SyncModeKind value) => value switch
    {
        SyncModeKind.Simple => "Простой",
        SyncModeKind.MultiBeam => "Многолучевой",
        SyncModeKind.MultiChannel => "Многоканальный",
        SyncModeKind.DualPolarization => "2П",
        _ => "неопределено",
    };

    /// <summary>Подпись единицы измерения.</summary>
    public static string Of(DistanceUnit unit) => unit switch
    {
        DistanceUnit.Degrees => "°",
        DistanceUnit.Meters => " м",
        _ => string.Empty,
    };
}
