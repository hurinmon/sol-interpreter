namespace Interpreter
{
    public class InvalidSyntaxException : Exception
    {
        public InvalidSyntaxException() : base("") { }
        public InvalidSyntaxException(Lexer lexer, string message) : base($"line : {lexer.Line} {message}") { }
    }
}
