using ExportMaster.Core;
using ExportMaster.Engine.Rendering;
using ExportMaster.Template.Diagnostics;
using ExportMaster.Template.Parsing;

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

        if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return Failed(request, diagnostics, "Заголовок шаблона содержит ошибки; см. журнал.");
        }

        var resolver = new StubValueResolver(request.StubStyle);
        var renderer = new TemplateRenderer(resolver);
        var context = new RenderContext(header.Output);

        var key = BuildKey(header.GroupBy);
        var fileName = header.BuildFileName(renderer, context, diagnostics);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Failed(request, diagnostics, "Поле FILENAME дало пустое имя файла.");
        }

        var content = renderer.Render(parsed.Document.Body, context, diagnostics);

        if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return new FormatJobResult(
                [ResultLine.Failure(request.Id, key, fileName, "При формировании файла возникли ошибки; см. журнал.")],
                diagnostics,
                completed: true);
        }

        var path = Path.Combine(request.TargetDirectory, fileName);

        if (File.Exists(path) && !request.Overwrite)
        {
            return new FormatJobResult(
                [ResultLine.Failure(request.Id, key, fileName, "Файл уже существует, перезапись запрещена ключом /overwrite.")],
                diagnostics,
                completed: true);
        }

        try
        {
            Directory.CreateDirectory(request.TargetDirectory);
            File.WriteAllText(path, content, header.Output.Encoding);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new FormatJobResult(
                [ResultLine.Failure(request.Id, key, fileName, $"Не удалось записать файл: {exception.Message}")],
                diagnostics,
                completed: true);
        }

        return new FormatJobResult(
            [ResultLine.Success(request.Id, key, fileName)],
            diagnostics,
            completed: true);
    }

    /// <summary>
    /// Ключ результирующего файла. Значения осей приходят из файлов данных, которые
    /// на этом этапе не читаются, поэтому вместо каждого стоит звёздочка.
    /// </summary>
    private static string BuildKey(IReadOnlyList<Dimensions> groupBy) =>
        string.Join(';', groupBy.Select(axis => $"{axis}=*"));

    private static FormatJobResult Failed(FormatRequest request, List<Diagnostic> diagnostics, string message) =>
        new(
            [ResultLine.Failure(request.Id, string.Empty, string.Empty, message)],
            diagnostics,
            completed: false);
}
