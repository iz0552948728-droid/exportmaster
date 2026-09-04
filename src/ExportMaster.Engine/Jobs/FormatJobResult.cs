using ExportMaster.Template.Diagnostics;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Итог выполнения задания.
/// </summary>
public sealed class FormatJobResult
{
    internal FormatJobResult(IReadOnlyList<ResultLine> lines, IReadOnlyList<Diagnostic> diagnostics, bool completed)
    {
        Lines = lines;
        Diagnostics = diagnostics;
        Completed = completed;
    }

    /// <summary>Строки отчёта — по одной на каждый файл (ТЗ п. 4.3.3).</summary>
    public IReadOnlyList<ResultLine> Lines { get; }

    /// <summary>Замечания к шаблону и заданию; в стандартный вывод не идут.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Задание вообще удалось начать: шаблон прочитан и разобран. Если нет —
    /// файлов не будет ни одного, и это отличается от «часть файлов не удалась».
    /// </summary>
    public bool Completed { get; }

    /// <summary>Код возврата процесса: 0 — всё, 1 — частично, 2 — задание не выполнено.</summary>
    public int ExitCode
    {
        get
        {
            if (!Completed)
            {
                return 2;
            }

            return Lines.Any(line => line.Code != 0) ? 1 : 0;
        }
    }
}
