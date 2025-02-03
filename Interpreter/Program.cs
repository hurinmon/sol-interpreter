namespace Interpreter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var lexer = new Lexer("./", "test.sol");
            var interpreter = new Interpreter(lexer);
            using var _ = new OneTimeStopwatch("Excute Time");
            interpreter.Parse().GetAwaiter().GetResult();
        }
    }
}
