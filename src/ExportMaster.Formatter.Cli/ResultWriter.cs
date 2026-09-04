using System.Text;
using ExportMaster.Engine.Jobs;

namespace ExportMaster.Formatter.Cli;

/// <summary>
/// Пишет строки отчёта в стандартный вывод (ТЗ п. 4.3.3).
/// </summary>
/// <remarks>
/// Единственное место в программе, которое пишет в стандартный вывод. Его разбирает
/// программа заказчика, и одно постороннее сообщение сломает разбор: вся диагностика
/// идёт в поток ошибок и в журнал.
/// </remarks>
public sealed class ResultWriter
{
    private readonly TextWriter _output;

    public ResultWriter(TextWriter output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>Пишет по строке на каждый сформированный файл.</summary>
    public void Write(IReadOnlyList<ResultLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        foreach (var line in lines)
        {
            _output.WriteLine(line.Format());
        }

        _output.Flush();
    }

    /// <summary>Настраивает стандартный вывод на UTF-8: в отчёте бывает кириллица.</summary>
    public static void ConfigureConsole()
    {
        try
        {
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
        catch (IOException)
        {
            // Вывод перенаправлен в место, не позволяющее менять кодировку, — не помеха.
        }
    }
}
