using ExportMaster.Template.Text;

namespace ExportMaster.Template.Parsing.Ast;

/// <summary>Аргумент специального поля.</summary>
public abstract class Argument
{
    private protected Argument(SourceSpan span) => Span = span;

    /// <summary>Место аргумента в файле шаблона.</summary>
    public SourceSpan Span { get; }
}

/// <summary>Строковый литерал; удвоенные кавычки раскрыты.</summary>
public sealed class StringArgument : Argument
{
    public StringArgument(string value, SourceSpan span)
        : base(span) => Value = value;

    public string Value { get; }

    public override string ToString() => $"\"{Value}\"";
}

/// <summary>Числовой литерал.</summary>
public sealed class NumberArgument : Argument
{
    public NumberArgument(double value, string text, SourceSpan span)
        : base(span)
    {
        Value = value;
        Text = text;
    }

    public double Value { get; }

    /// <summary>Запись числа в шаблоне — нужна для точных сообщений об ошибках.</summary>
    public string Text { get; }

    public override string ToString() => Text;
}

/// <summary>
/// Имя без скобок: ось в списке отбора данных, имя функции для <c>CALC</c>.
/// </summary>
public sealed class IdentifierArgument : Argument
{
    public IdentifierArgument(string name, SourceSpan span)
        : base(span) => Name = name;

    public string Name { get; }

    public override string ToString() => Name;
}

/// <summary>Вложенное специальное поле в качестве аргумента.</summary>
public sealed class CallArgument : Argument
{
    public CallArgument(CallNode call)
        : base(call.Span) => Call = call;

    public CallNode Call { get; }

    public override string ToString() => Call.ToString();
}
