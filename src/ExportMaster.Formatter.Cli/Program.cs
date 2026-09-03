namespace ExportMaster.Formatter.Cli;

/// <summary>
/// Точка входа модуля форматирования в режиме командной строки.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        // Разбор аргументов, рендер и протокол вывода появятся на следующих этапах.
        // Стандартный вывод принадлежит протоколу ТЗ п. 4.3.3 — диагностика идёт в stderr.
        Console.Error.WriteLine("ExportMaster: модуль форматирования пока не реализован.");
        return 2;
    }
}
