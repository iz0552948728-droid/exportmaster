using System.Text;
using ExportMaster.Template.Parsing.Ast;

namespace ExportMaster.Engine.Rendering;

/// <summary>
/// Восстанавливает запись специального поля по дереву.
/// </summary>
/// <remarks>
/// Нужно заглушкам и сообщениям об ошибках: автор шаблона должен узнать в выводе
/// то, что сам написал, а не внутреннее представление.
/// </remarks>
public static class CallText
{
    /// <summary>Собирает запись поля вида <c>FORMAT("0.00", FIELD("M", "P"))</c>.</summary>
    public static string Format(CallNode call)
    {
        var builder = new StringBuilder();
        Append(builder, call);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, CallNode call)
    {
        builder.Append(call.Name).Append('(');

        for (var index = 0; index < call.Arguments.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            Append(builder, call.Arguments[index]);
        }

        builder.Append(')');
    }

    private static void Append(StringBuilder builder, Argument argument)
    {
        switch (argument)
        {
            case StringArgument text:
                builder.Append('"').Append(text.Value.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
                break;

            case NumberArgument number:
                builder.Append(number.Text);
                break;

            case IdentifierArgument identifier:
                builder.Append(identifier.Name);
                break;

            case CallArgument call:
                Append(builder, call.Call);
                break;
        }
    }
}
