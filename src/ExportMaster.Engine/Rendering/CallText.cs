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

    /// <summary>
    /// Короткая запись поля для имени файла: имена из аргументов через точку.
    /// <c>FIELD("MEAS", "NAME")</c> превращается в <c>MEAS.NAME</c>.
    /// </summary>
    /// <remarks>
    /// Полная запись со скобками и кавычками даёт нечитаемое имя, а кавычки
    /// в именах файлов Windows не допускает вовсе.
    /// </remarks>
    public static string Compact(CallNode call)
    {
        ArgumentNullException.ThrowIfNull(call);

        var parts = new List<string>();

        foreach (var argument in call.Arguments)
        {
            switch (argument)
            {
                case StringArgument text when !string.IsNullOrWhiteSpace(text.Value):
                    parts.Add(text.Value);
                    break;

                case IdentifierArgument identifier:
                    parts.Add(identifier.Name);
                    break;

                case CallArgument nested:
                    parts.Add(Compact(nested.Call));
                    break;
            }
        }

        return parts.Count == 0 ? call.Name : string.Join('.', parts);
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
