namespace ExportMaster.Engine.Rendering;

/// <summary>Вид заглушки, подставляемой вместо значения.</summary>
public enum StubStyle
{
    /// <summary>
    /// Описание поля: видно, что именно будет подставлено. Годится для вычитки
    /// шаблона глазами.
    /// </summary>
    Descriptive,

    /// <summary>
    /// Правдоподобное число, отформатированное строкой формата из шаблона.
    /// Выходной файл открывается в Excel, и структуру видно как есть.
    /// </summary>
    Sample,
}
