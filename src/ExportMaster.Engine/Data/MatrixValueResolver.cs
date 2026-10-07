using System.Numerics;
using ExportMaster.Core;
using ExportMaster.Engine.Rendering;
using ExportMaster.Formats;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing.Ast;
using ExportMaster.Template.Text;

namespace ExportMaster.Engine.Data;

/// <summary>
/// Достаёт значения из файлов измерений.
/// </summary>
public sealed class MatrixValueResolver : IValueResolver
{
    private const string ValueField = "VALUE";
    private const string GroupValueField = "GROUPVALUE";
    private const string HeaderFieldName = "FIELD";
    private const string FormatField = "FORMAT";
    private const string NumberFormat = "G6";

    private readonly MatrixFile _matrix;
    private readonly List<Diagnostic> _diagnostics = [];
    private readonly HashSet<SourceSpan> _reported = [];

    public MatrixValueResolver(MatrixFile matrix)
    {
        _matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
    }

    /// <summary>Замечания, накопленные при подстановке значений.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public ResolvedValue Resolve(CallNode call, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(context);

        return call.Name switch
        {
            GroupValueField => ResolveGroupValue(call, context),
            ValueField => ResolveValue(call, context),
            HeaderFieldName => ResolveHeaderField(call, context),
            FormatField => ResolveUnformattable(call, context),
            _ => ResolveQuantity(call, context),
        };
    }

    public int AxisLength(Dimensions axis, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var position = context.Slice?.Matrix.IndexOfAxis(axis) ?? -1;
        return position < 0 ? 0 : context.Slice!.Matrix.Axes[position].Length;
    }

    public string AxisHeader(Dimensions axis, int index, RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var position = context.Slice?.Matrix.IndexOfAxis(axis) ?? -1;

        return position < 0
            ? string.Empty
            : new AxisValueFormatter(context.Settings).Format(context.Slice!.Matrix.Axes[position], index);
    }

    public ResolvedValue Cell(
        string function,
        Dimensions columnAxis,
        int column,
        Dimensions rowAxis,
        int row,
        RenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Slice is not { } slice)
        {
            return ResolvedValue.FromText(string.Empty);
        }

        var indices = slice.BuildIndices([(columnAxis, column), (rowAxis, row)]);
        return ApplyQuantity(function, slice.Matrix.ValueAt(indices), context, default);
    }

    /// <summary>Текущее значение оси разбиения (ТЗ п. 4.4.5).</summary>
    private ResolvedValue ResolveGroupValue(CallNode call, RenderContext context)
    {
        var axis = ReadAxis(call.Arguments.Count == 1 ? call.Arguments[0] : null);

        if (axis is null)
        {
            Report(call, $"Поле {GroupValueField} принимает ровно одно имя оси.");
            return Unresolved(call, context);
        }

        var descriptor = context.Slice?.DescriptorOf(axis.Value);

        if (descriptor is null)
        {
            Report(call, $"Ось {axis} не является осью разбиения, текущего значения у неё нет.");
            return Unresolved(call, context);
        }

        var formatter = new AxisValueFormatter(context.Settings);
        return ResolvedValue.FromText(formatter.Format(descriptor.Value.Descriptor, descriptor.Value.Index));
    }

    /// <summary>
    /// Одно значение матрицы по списку индексов.
    /// </summary>
    /// <remarks>
    /// У комплексных данных одного числа нет, поэтому такое значение полагается
    /// оборачивать в величину — <c>AMP</c>, <c>PHASE</c> и тому подобные.
    /// </remarks>
    private ResolvedValue ResolveValue(CallNode call, RenderContext context)
    {
        if (!TryReadPoint(call, out var matrix, out var value))
        {
            return Unresolved(call, context);
        }

        if (matrix.Nature == DataNature.Complex)
        {
            Report(
                call,
                "Данные комплексные: укажите величину, например AMP(...) или PHASE(...).");

            return Unresolved(call, context);
        }

        return ResolvedValue.FromNumber(value.Real, value.Real.ToString(NumberFormat, context.Settings.NumberFormat));
    }

    /// <summary>
    /// Рендерер обращается к источнику значений за полем <c>FORMAT</c> только тогда,
    /// когда форматировать оказалось нечего: значение внутри не является числом.
    /// На заглушках это нормальный исход, на реальных данных — ошибка.
    /// </summary>
    private ResolvedValue ResolveUnformattable(CallNode call, RenderContext context)
    {
        Report(call, $"Значение внутри {FormatField} не является числом, форматировать нечего.");
        return Unresolved(call, context);
    }

    /// <summary>
    /// Поле блока данных заголовка: <c>FIELD(&lt;имя поля&gt;)</c> (ТЗ пп. 4.2.1.6, 4.4.3.3).
    /// </summary>
    private ResolvedValue ResolveHeaderField(CallNode call, RenderContext context)
    {
        if (call.Arguments.Count != 1)
        {
            Report(call, $"Поле {HeaderFieldName} принимает ровно одно имя поля заголовка.");
            return Unresolved(call, context);
        }

        var name = call.Arguments[0] switch
        {
            StringArgument text => text.Value,
            IdentifierArgument identifier => identifier.Name,
            _ => null,
        };

        if (!HeaderFields.TryParse(name, out var field))
        {
            Report(call, $"'{name}' не является полем заголовка; имена перечислены в ТЗ п. 4.2.1.6.");
            return Unresolved(call, context);
        }

        var value = new HeaderFieldFormatter(context.Settings).Format(_matrix.Header, field, out var warning);

        if (warning is not null)
        {
            Report(call, warning, DiagnosticSeverity.Warning);
        }

        return value;
    }

    /// <summary>Величина, вычисленная от значения матрицы: AMP, PHASE и прочие.</summary>
    private ResolvedValue ResolveQuantity(CallNode call, RenderContext context)
    {
        if (!QuantityFunctions.IsKnown(call.Name))
        {
            Report(call, $"Неизвестная функция '{call.Name}'.");
            return Unresolved(call, context);
        }

        if (call.Arguments.Count != 1
            || call.Arguments[0] is not CallArgument inner
            || !string.Equals(inner.Call.Name, ValueField, StringComparison.Ordinal))
        {
            Report(call, $"Функция {call.Name} принимает одно значение матрицы: {ValueField}(...).");
            return Unresolved(call, context);
        }

        return TryReadPoint(inner.Call, out _, out var value)
            ? ApplyQuantity(call.Name, value, context, call.Span)
            : Unresolved(call, context);
    }

    /// <summary>
    /// Читает точку матрицы по алиасу и списку индексов.
    /// </summary>
    /// <remarks>
    /// Индексы перечисляются позиционно по номерам типов значений 1…8, как в примере
    /// ТЗ п. 4.4.3.3. На местах осей, отсутствующих в файле, должен стоять ноль.
    /// </remarks>
    private bool TryReadPoint(CallNode call, out MatrixFile matrix, out Complex value)
    {
        matrix = _matrix;
        value = default;

        var axes = Enum.GetValues<Dimensions>();

        if (call.Arguments.Count != axes.Length)
        {
            Report(call, $"Поле {ValueField} ожидает {axes.Length} индексов — по одному на каждый тип значения.");
            return false;
        }

        var indices = new int[matrix.Axes.Count];

        for (var position = 0; position < axes.Length; position++)
        {
            if (call.Arguments[position] is not NumberArgument number)
            {
                Report(call, $"Индексы в поле {ValueField} должны быть числами.");
                return false;
            }

            var index = (int)number.Value;
            var axisPosition = matrix.IndexOfAxis(axes[position]);

            if (axisPosition < 0)
            {
                if (index != 0)
                {
                    Report(call, $"Оси {axes[position]} в файле нет, поэтому её индекс должен быть нулевым.");
                    return false;
                }

                continue;
            }

            if (index < 0 || index >= matrix.Axes[axisPosition].Length)
            {
                Report(
                    call,
                    $"Индекс {index} выходит за пределы оси {axes[position]}: "
                        + $"в ней {matrix.Axes[axisPosition].Length} точек.");

                return false;
            }

            indices[axisPosition] = index;
        }

        value = matrix.ValueAt(indices);
        return true;
    }

    private ResolvedValue ApplyQuantity(string function, Complex value, RenderContext context, SourceSpan span)
    {
        var result = QuantityFunctions.Evaluate(function, value);

        if (result is null)
        {
            _diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedFieldName,
                $"Неизвестная функция '{function}'.",
                span));

            return ResolvedValue.FromText(string.Empty);
        }

        return ResolvedValue.FromNumber(
            result.Value,
            result.Value.ToString(NumberFormat, context.Settings.NumberFormat));
    }

    /// <summary>
    /// Значение подставить не удалось. В имени файла используется короткая форма:
    /// скобки и кавычки полной записи в имена файлов не годятся.
    /// </summary>
    private static ResolvedValue Unresolved(CallNode call, RenderContext context) =>
        ResolvedValue.FromText(
            context.CompactStubs
                ? CallText.Compact(call)
                : $"‹{CallText.Format(call)}›");

    /// <summary>
    /// Добавляет замечание.
    /// </summary>
    /// <remarks>
    /// Предупреждение сообщается один раз на место в шаблоне: при разбиении одно
    /// и то же поле вычисляется на каждую вырезку, и журнал заполнился бы повторами.
    /// Ошибка сообщается всякий раз — по ней решается судьба каждого отдельного файла.
    /// </remarks>
    private void Report(CallNode call, string message, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        if (severity != DiagnosticSeverity.Error && !_reported.Add(call.Span))
        {
            return;
        }

        _diagnostics.Add(new Diagnostic(severity, DiagnosticCode.ExpectedArgument, message, call.Span));
    }

    private static Dimensions? ReadAxis(Argument? argument)
    {
        var name = argument switch
        {
            StringArgument text => text.Value,
            IdentifierArgument identifier => identifier.Name,
            _ => null,
        };

        return name is not null && Enum.TryParse<Dimensions>(name, ignoreCase: false, out var axis)
            ? axis
            : null;
    }
}
