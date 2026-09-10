namespace ExportMaster.Formats;

/// <summary>
/// Файл данных не удалось разобрать.
/// </summary>
public sealed class MatrixFormatException : Exception
{
    public MatrixFormatException(string message)
        : base(message)
    {
    }

    public MatrixFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
