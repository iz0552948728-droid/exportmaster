using ExportMaster.Core;
using ExportMaster.Engine.Data;
using ExportMaster.Engine.Rendering;
using ExportMaster.Formats;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Jobs;

/// <summary>
/// Выполняет задание модуля форматирования.
/// </summary>
/// <remarks>
/// Ошибки не выбрасываются наружу: неудача даёт строку отчёта с кодом 1, а не падение
/// всего задания (ТЗ п. 4.7 — отказы не допускаются).
/// </remarks>
public sealed class FormattingService
{
    /// <summary>Выполняет задание и возвращает строки отчёта.</summary>
    public FormatJobResult Execute(FormatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<Diagnostic>();

        string template;
        try
        {
            template = File.ReadAllText(request.TemplatePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed(request, diagnostics, $"Не удалось прочитать шаблон: {exception.Message}");
        }

        var parsed = TemplateParser.Parse(template);
        diagnostics.AddRange(parsed.Diagnostics);

        if (parsed.HasErrors)
        {
            return Failed(request, diagnostics, "Шаблон содержит ошибки; см. журнал.");
        }

        var header = HeaderSettings.Parse(parsed.Document.Header, diagnostics);
        SemanticValidator.Validate(parsed.Document, header.GroupBy, diagnostics);

        if (HasErrors(diagnostics))
        {
            return Failed(request, diagnostics, "Шаблон содержит ошибки; см. журнал.");
        }

        Dictionary<string, MatrixFile> matrices;
        try
        {
            matrices = LoadMatrices(request.Sources);
        }
        catch (MatrixFormatException exception)
        {
            return Failed(request, diagnostics, exception.Message);
        }

        if (matrices.Count == 0)
        {
            // Файлы данных не заданы: формируется один файл с заглушками.
            var stub = new StubValueResolver(request.StubStyle);
            return Render(request, header, parsed.Document, stub, slice: null, diagnostics);
        }

        if (matrices.Count > 1)
        {
            return Failed(
                request,
                diagnostics,
                "Задано несколько матриц. Пока поддерживается одна: неясно, по осям какой из них вести разбиение.");
        }

        var matrix = matrices.Values.Single();

        if (!ValidateAgainstMatrix(matrix, header.GroupBy, diagnostics))
        {
            return Failed(request, diagnostics, "Шаблон не соответствует файлу данных; см. журнал.");
        }

        var resolver = new MatrixValueResolver(matrices);
        var lines = new List<ResultLine>();

        foreach (var slice in GroupIterator.Enumerate(matrix, header.GroupBy))
        {
            var result = Render(request, header, parsed.Document, resolver, slice, diagnostics);
            lines.AddRange(result.Lines);
        }

        diagnostics.AddRange(resolver.Diagnostics);
        return new FormatJobResult(lines, diagnostics, completed: true);
    }

    /// <summary>
    /// Формирует один выходной файл — по вырезке либо по заглушкам.
    /// </summary>
    private static FormatJobResult Render(
        FormatRequest request,
        HeaderSettings header,
        TemplateDocument document,
        IValueResolver resolver,
        GroupSlice? slice,
        List<Diagnostic> diagnostics)
    {
        var renderer = new TemplateRenderer(resolver);
        var context = new RenderContext(header.Output) { Slice = slice };
        var key = slice?.Key ?? string.Join(';', header.GroupBy.Select(axis => $"{axis}=*"));

        var local = new List<Diagnostic>();
        var fileName = header.BuildFileName(renderer, context, local);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            diagnostics.AddRange(local);
            return Single(ResultLine.Failure(request.Id, key, string.Empty, "Поле FILENAME дало пустое имя файла."), diagnostics);
        }

        var content = renderer.Render(document.Body, context, local);
        diagnostics.AddRange(local);

        if (HasErrors(local))
        {
            return Single(
                ResultLine.Failure(request.Id, key, fileName, "При формировании файла возникли ошибки; см. журнал."),
                diagnostics);
        }

        var path = Path.Combine(request.TargetDirectory, fileName);

        if (File.Exists(path) && !request.Overwrite)
        {
            return Single(
                ResultLine.Failure(request.Id, key, fileName, "Файл уже существует, перезапись запрещена ключом /overwrite."),
                diagnostics);
        }

        try
        {
            Directory.CreateDirectory(request.TargetDirectory);
            File.WriteAllText(path, content, header.Output.Encoding);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Single(
                ResultLine.Failure(request.Id, key, fileName, $"Не удалось записать файл: {exception.Message}"),
                diagnostics);
        }

        return Single(ResultLine.Success(request.Id, key, fileName), diagnostics);
    }

    /// <summary>
    /// Сверяет оси шаблона с описателями файла: то, что нельзя проверить без данных.
    /// </summary>
    private static bool ValidateAgainstMatrix(
        MatrixFile matrix,
        IReadOnlyList<Dimensions> groupBy,
        List<Diagnostic> diagnostics)
    {
        var before = diagnostics.Count;

        foreach (var axis in groupBy)
        {
            if (matrix.IndexOfAxis(axis) < 0)
            {
                diagnostics.Add(Diagnostic.Error(
                    DiagnosticCode.ExpectedArgument,
                    $"Ось разбиения {axis} в файле данных отсутствует. В нём есть: "
                        + $"{string.Join(", ", matrix.Axes.Select(a => a.Type))}.",
                    default));
            }
        }

        // Разбиение обязано оставить ровно два измерения — они и становятся
        // строками и колонками таблицы.
        var expected = matrix.Axes.Count - 2;

        if (groupBy.Count != expected)
        {
            diagnostics.Add(Diagnostic.Error(
                DiagnosticCode.ExpectedArgument,
                $"Матрица имеет {matrix.Axes.Count} измерений, поэтому GROUPBY должен содержать "
                    + $"{expected} осей, а содержит {groupBy.Count}.",
                default));
        }

        return diagnostics.Count == before;
    }

    private static Dictionary<string, MatrixFile> LoadMatrices(IReadOnlyDictionary<string, string> sources)
    {
        var matrices = new Dictionary<string, MatrixFile>(StringComparer.Ordinal);

        foreach (var (alias, path) in sources)
        {
            matrices[alias] = MatrixReader.Read(path);
        }

        return matrices;
    }

    private static bool HasErrors(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    private static FormatJobResult Single(ResultLine line, List<Diagnostic> diagnostics) =>
        new([line], diagnostics, completed: true);

    private static FormatJobResult Failed(FormatRequest request, List<Diagnostic> diagnostics, string message) =>
        new(
            [ResultLine.Failure(request.Id, string.Empty, string.Empty, message)],
            diagnostics,
            completed: false);
}
