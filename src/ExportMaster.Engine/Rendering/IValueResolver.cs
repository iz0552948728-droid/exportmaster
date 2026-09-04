using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Источник значений для специальных полей.
/// </summary>
/// <remarks>
/// Главный шов конструкции. На этапе заглушек подставляется
/// <see cref="StubValueResolver"/>, на этапе реальных данных — реализация,
/// читающая файлы измерений. Разбор шаблона, рендерер, командная строка и
/// формат вывода при этой замене не меняются.
/// </remarks>
public interface IValueResolver
{
    /// <summary>Возвращает значение для указанного поля.</summary>
    ResolvedValue Resolve(CallNode call, RenderContext context);
}
