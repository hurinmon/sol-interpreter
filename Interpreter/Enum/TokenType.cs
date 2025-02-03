namespace Interpreter.Enum
{
    public enum TokenType
    {
        Eof, // end of file
        Assign, // =
        Decimal,
        Plus,
        Minus,
        Mul,
        Div,
        Lparen, // (
        Rparen, // )
        Id, // identifiers (variable names)
        Function,
        Comma,
        Semi,
        Lbrace,
        Rbrace,
        For,//for
        Foreach,//foreach
        In,
        GeaterThan,//>
        LessThan, //<
        SingleQuote, //'
        Return,
        If,
        True,
        False,
        Not, //!
        Modulo, //%
        DoubleSlash, // //
        LBlockComment,
        RBlockComment,
        Newline,
        Dot,
        Await,
        Async,
        Import,
        New,
        While,
        Break,
        Else,
        Condition,
    }
}
