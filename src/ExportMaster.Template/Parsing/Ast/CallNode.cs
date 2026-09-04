using ExportMaster.Template.Text;

namespace ExportMaster.Template.Parsing.Ast;

/// <summary>
/// Специальное поле: имя и список аргументов.
/// </summary>
/// <remarks>
/// Все поля — <c>FIELD</c>, <c>VALUE</c>, <c>NAME</c>, <c>CALC</c>, <c>FORMAT</c>,
/// <c>TABLE</c>, встроенные функции — представлены одним видом узла и различаются
/// только именем. Парсер намеренно не знает списка допустимых имён: он задаётся
/// реестром на этапе проверки, и новое поле не требует правки разбора.
/// </remarks>
public sealed class CallNode : TemplateNode
{
    public CallNode(string name, IReadOnlyList<Argument> arguments, SourceSpan span)
        : base(span)
    {
        Name = name;
        Arguments = arguments;
    }

    /// <summary>Имя поля или функции в том виде, в каком оно записано в шаблоне.</summary>
    public string Name { get; }

    /// <summary>Аргументы в порядке следования.</summary>
    public IReadOnlyList<Argument> Arguments { get; }

    public override string ToString() => $"{Name}({Arguments.Count})";
}
