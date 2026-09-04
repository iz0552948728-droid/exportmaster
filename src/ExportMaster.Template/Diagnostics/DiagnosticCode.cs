namespace ExportMaster.Template.Diagnostics;

/// <summary>
/// Код замечания. Тесты и вызывающий код опираются на код, а не на текст сообщения:
/// формулировки будут меняться, коды — нет.
/// </summary>
public enum DiagnosticCode
{
    /// <summary>Заголовок шаблона отсутствует (ТЗ п. 4.4.1).</summary>
    MissingHeader = 1,

    /// <summary>Встречен <c>[MDHEADER]</c> без парного <c>[/MDHEADER]</c>.</summary>
    UnterminatedHeader = 2,

    /// <summary>В заголовке встречен текст вне специального поля.</summary>
    UnexpectedTextInHeader = 3,

    /// <summary>Специальное поле не закрыто до конца файла.</summary>
    UnterminatedField = 4,

    /// <summary>Строковый литерал не закрыт кавычкой.</summary>
    UnterminatedString = 5,

    /// <summary>Закрывающая фигурная скобка без открывающей.</summary>
    UnbalancedClosingBrace = 6,

    /// <summary>После открывающей скобки ожидалось имя поля.</summary>
    ExpectedFieldName = 7,

    /// <summary>После имени поля ожидалась круглая скобка.</summary>
    ExpectedOpeningParenthesis = 8,

    /// <summary>Список аргументов не закрыт круглой скобкой.</summary>
    ExpectedClosingParenthesis = 9,

    /// <summary>На месте аргумента встречено нечто иное.</summary>
    ExpectedArgument = 10,

    /// <summary>Специальное поле не закрыто фигурной скобкой.</summary>
    ExpectedClosingBrace = 11,

    /// <summary>Символ недопустим в этом месте.</summary>
    UnexpectedCharacter = 12,

    /// <summary>Числовой литерал записан неверно.</summary>
    InvalidNumber = 13,
}
