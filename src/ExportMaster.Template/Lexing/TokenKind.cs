namespace ExportMaster.Template.Lexing;

/// <summary>Вид лексемы шаблона.</summary>
public enum TokenKind
{
    /// <summary>Участок исходного текста вне полей; экранирование уже раскрыто.</summary>
    Text,

    /// <summary>Открывающая фигурная скобка — начало специального поля.</summary>
    LeftBrace,

    /// <summary>Закрывающая фигурная скобка — конец специального поля.</summary>
    RightBrace,

    /// <summary>Открывающая круглая скобка.</summary>
    LeftParenthesis,

    /// <summary>Закрывающая круглая скобка.</summary>
    RightParenthesis,

    /// <summary>Запятая между аргументами.</summary>
    Comma,

    /// <summary>Имя поля, функции или оси.</summary>
    Identifier,

    /// <summary>Строковый литерал; удвоенные кавычки уже раскрыты.</summary>
    String,

    /// <summary>Числовой литерал.</summary>
    Number,

    /// <summary>Конец разбираемого участка.</summary>
    EndOfFile,
}
